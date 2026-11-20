# Inventory API: observed behaviour

What the upstream Inventory API (`GET /api/records`) actually does, measured by
probing it on 2026-11-20. The assignment says the API is "not perfectly well
behaved"; this is the list of ways, and each one is something the service has
to handle.

These are observations, not guarantees. The API's behaviour may change, so the
client validates what it receives rather than trusting this list.

## Summary

| # | Finding | Detail | Why it matters |
|---|---------|--------|----------------|
| 1 | **Auth** | The key goes in an `X-Api-Key` header. A missing or wrong key returns 401. | A 401 will never succeed on retry, so it must fail fast. |
| 2 | **Rate limit** | 5 requests per 10 seconds per key, then 429 with a `Retry-After` header in seconds. | A full fetch needs 7 pages at the largest page size, so every full fetch hits the limit. |
| 3 | **Repeated records** | Every even-numbered page starts with a copy of the previous page's last record. | Summing rows as received counts some records twice, and "rows received = `totalRecords`" is false even on a perfect run. |
| 4 | **Messy categories** | 36 spellings of 6 categories, differing only in letter case and surrounding spaces. | Grouping on the raw text gives 36 categories instead of 6. |
| 5 | **Zero stock** | 21 of 157 records have quantity 0, including all 7 in the "Discontinued" category. | "Discontinued" has no available stock, so it does not appear in the output. |
| 6 | **Past the last page** | Returns 200 with an empty `records` list, not a 400. | A loop that runs "until an error" never gets one; stop on `totalPages`. |

## Details

### 1. Auth

- No key, or a wrong key: `401` with a problem-details body,
  `"A valid API key is required in the X-Api-Key header."`
- Requests with a wrong key do not count against the real key's rate limit.

### 2. Rate limit

- The 429 body says `"Rate limit of 5 requests per 10 seconds exceeded."`
- `Retry-After` is a whole number of seconds and counts down as the window
  runs out (10, 9, 8, ...). Waiting that long and retrying succeeded every time.
- It is a **fixed window**, not a sliding one: 1 request at 0s, 4 at 6s and
  5 more at 10.6s all succeeded.
- Requests rejected with 429 do not extend the window.
- Requests that return 400 **do** count against the limit.

### 3. Repeated records

| Page size | Pages | Rows returned | Unique ids | Repeated ids |
|-----------|-------|---------------|------------|--------------|
| 25 | 7 | 160 | 157 | 1025, 1075, 1125 |
| 20 | 8 | 161 | 157 | 1020, 1060, 1100, 1140 |

- The repeated copies are identical to the originals in every field.
- No record is missing: ids run 1001 to 1157 with no gaps, and the unique
  count equals `totalRecords` (157).
- Even pages therefore carry one more record than `pageSize`.
- The pattern held at every page size tested (1, 3, 5, 7, 10, 15, 20, 24
  and 25): every even page repeats one record, and no record is ever missing.

### 4. Messy categories

The six real categories, after trimming spaces and ignoring case:
Electronics, Home & Kitchen, Office Supplies, Outdoor, Toys, Discontinued.

Examples of raw values for one category: `"Home & Kitchen"`,
`" Home & Kitchen"`, `"  home & kitchen "`, `"HOME & KITCHEN"`.

### 5. Zero stock

- 21 records have `quantity` 0. All 7 "Discontinued" records are among them.
- No record has a negative quantity.

### 6. Request validation

| Request | Response |
|---------|----------|
| `page=0` or `page=abc` | 400, `"page must be an integer of 1 or greater."` |
| `pageSize=26` | 400, `"pageSize must be an integer from 1 to 25."` |
| `page=8&pageSize=25` (one past the end) | 200, `"records": []` |
| no parameters | 200, page 1 at page size 20 |

## What was clean

- Every record has all six fields with the documented types.
- Prices are positive with at most two decimal places.
- Every `lastUpdated` has the form `2026-09-18T21:02:00Z`.
- No two available records in the same category tie for most recently updated.
- The same request returned byte-identical data on every run.
- `page`, `pageSize`, `totalPages` and `totalRecords` in the response always
  matched the request and each other.

## Not observed, but still handled

- **500 Internal Server Error**: documented by the assignment, never seen in
  about 300 requests. The client must still handle it.
- **Timeouts and dropped connections**: never seen; responses took 0.1 to 0.25
  seconds.
- **A malformed 200** (invalid JSON, missing fields, wrong page echoed back):
  never seen. A slow sweep of 169 requests across nine page sizes returned
  169 well-formed 200s.

## Expected results

Computed independently from the probed data (a throwaway script, not the
service), to check the service's output against:

| Category | Available records | Total inventory value | Most recently updated (id) |
|----------|-------------------|-----------------------|----------------------------|
| Electronics | 25 | 18253.18 | 1037 |
| Home & Kitchen | 27 | 17151.51 | 1019 |
| Office Supplies | 30 | 15996.70 | 1091 |
| Outdoor | 27 | 17822.93 | 1073 |
| Toys | 27 | 15879.52 | 1055 |
