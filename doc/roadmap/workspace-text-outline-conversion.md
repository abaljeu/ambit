# Workspace Text Outline Conversion

Status: Draft
Authority: Target design for converting between plain text and outline structure.
See also: [[doc/roadmap/workspace-file-model.md]], [[doc/roadmap/workspace-file-persistence.md]], [[doc/roadmap/workspace-stage-plan.md]]

This document defines the separate conversion step used by the main import and export process. The workflow itself stays in the main process docs; this file only defines how text content becomes outline structure and how outline structure becomes text content again.

## Scope

This spec covers the conversion boundary between a text file and an outline tree.  It does not define desktop transfer, server persistence, or user command flow.

## Bidirectional Transformation

The two conversions form a paired bidirectional transformation, not two independent functions. Treat them as a lens:

- `toOutline`: text content becomes outline structure (the `get` direction)
- `toText`: outline structure becomes text content (the `put` direction)

All definitions below must heed these objectives:

1. **Round-trip stability**: `toText (toOutline text)` returns the original text for any text the converter accepts. `toOutline (toText outline)` returns the original outline for any outline the converter accepts.
2. **No silent loss**: any content or structure that cannot survive a round trip must be reported explicitly, never dropped or altered without notice.
3. **Paired rules**: a rule added to one direction must have a defined counterpart in the other direction, so the two stay consistent.
4. **Determinism**: each direction produces one defined result for a given input.

These objectives govern "Text To Outline", "Outline To Text", and "Round Trip Expectations" below.


## Text To Outline

Given text content, the converter produces an outline structure. This is the `get` direction of the bidirectional pair.

The converter must define:

1. how lines map to nodes
2. how indentation or syntax determines parentage
3. how empty lines and blank content are handled
4. how invalid text is reported
5. which `toText` rule each of the above pairs with

## Outline To Text

Given an outline tree, the converter produces text content. This is the `put` direction of the bidirectional pair.

The converter must define:

1. how node hierarchy becomes line order
2. how node content becomes text
3. how indentation is emitted
4. how unsupported structures are handled
5. which `toOutline` rule each of the above pairs with

## Round Trip Expectations

The conversion is defined as a paired unit so that import and export use the same rules in opposite directions. The round-trip laws stated under "Bidirectional Transformation" are the acceptance criteria for this pairing:

- `toText (toOutline text)` equals the original accepted text
- `toOutline (toText outline)` equals the original accepted outline
- anything that cannot satisfy these laws is reported, not silently changed

## Non-Goals

- desktop file transfer mechanics
- server write ordering
- workspace identity and persistence
- unrelated graph operations

## Verification Targets

- text input converts to a deterministic outline structure
- outline structure converts back to deterministic text
- `toText (toOutline text)` round-trips to the original accepted text
- `toOutline (toText outline)` round-trips to the original accepted outline
- content that cannot round-trip is reported, not dropped or altered
- invalid text is reported explicitly
- unsupported outline shapes are handled explicitly
