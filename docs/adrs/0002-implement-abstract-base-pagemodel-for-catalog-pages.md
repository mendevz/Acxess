# 2. Implement Abstract Base PageModel for Catalog Pages

Date: 2026-03-07

## Status

Accepted

## Context
Currently, catalog administration pages (e.g. Pages/AccesTiers, Pges/AddOns). duplicate a significant amount of boilerplate code. HTMX partial view rendering (_ErrorState, _Form), and basic state managment. This violates the DRY principles, increases the risk of UI bugs. 

## Decision

We will extract the repetitive UI and HTMX plumbing into a generic abstract base class (e.g., CatalogCrudPageModel<TDto, TInput>). Future catalog pages will inherit from this base class and will only be responsible for providing their specific MediatR commands and queries.

## Consequences

* Positive: Significant reduction in boilerplate code. FAster development cycle for new catalog pages.
* Negative: UI Flows become tightly coupled to the base class. 
