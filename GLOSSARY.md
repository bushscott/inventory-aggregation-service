# Inventory Aggregation

A service that reads inventory records from the Inventory API and reports them
grouped by category.

## Language

**Inventory API**:
The external, read-only, paginated service that owns the inventory records.
_Avoid_: Upstream, source API, backend

**Record**:
One product line in the inventory, identified by a unique id, with a name, a category, a unit price, a quantity on hand and a last-updated time.
_Avoid_: Item, product, row, entry

**Repeated Record**:
A record that the Inventory API returns more than once. The repeats seen so far are on different pages, but a repeat within one page counts too. Identical copies are the same record; copies that disagree are an error.
_Avoid_: Duplicate, dupe

**Dataset**:
The complete set of unique records held by the Inventory API, as collected across all of its pages.
_Avoid_: Result set, payload, data

**Available Record**:
A record with a quantity greater than zero. Only available records contribute to any reported figure.
_Avoid_: In-stock item, active record

**Category**:
One of a fixed, known list of groupings that every record belongs to exactly one of. The Inventory API spells a category's name inconsistently; the service recognises it regardless of letter case and surrounding whitespace.
_Avoid_: Group, type, department

**Unknown Category**:
Category text on a record that matches nothing in the known list. It is an error, not a new category.

**Category Summary**:
The figures reported for one category: its total inventory value, its available record count, its most recently updated record, and the available records those figures were computed from.
_Avoid_: Aggregate, rollup, bucket
