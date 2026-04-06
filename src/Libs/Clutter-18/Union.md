# Union in gir.core

## What is a union?

A `union` is a data type known from the C language where multiple fields share the same memory location. This means a union represents a single underlying memory block that can be interpreted in different ways, but only one representation is valid at a time.

On the GObject Introspection side, `union` is a distinct model element, just like `class`, `record`, `interface`, `enumeration`, or `callback`. In GIR, a union typically describes native types where memory layout is more important than object-oriented behavior.

## What is the role of union in this repository?

The gir.core generator is responsible for producing a C# API from GIR definitions. In this process, unions are required to create safe managed wrappers for certain native types.

This becomes critical in callbacks and function parameters where the GIR defines a union type. Without union support:

- public delegates cannot be generated correctly,
- parameter and return value conversion breaks,
- the generated public API becomes incomplete,
- the build fails later at a seemingly unrelated point.

A concrete example was `Clutter.Event`, which is a union in GIR. Because of this, the generation of the public `Clutter.EventFilterFunc` delegate previously resulted in an empty file.

## Where does it appear in the generator?

Union support appears across multiple layers:

1. Model layer
   The GIR loader and shared model treat unions as a distinct type.

2. Generation entry points
   The generator can emit union-specific files, similarly to records or classes.

3. Public wrapper generation
   If a union is usable publicly, a C# wrapper is generated around a native handle.

4. Marshalling
   The generator must know how to convert between native unions and managed representations for parameters, return values, and callbacks.

## Current approach in gir.core

Union support is intentionally restricted.

Not every GIR union results in a public wrapper. Public union generation is only allowed for unions that have `TypeFunction` metadata, meaning they have stable GType-based identification.

The reason is that broad union support can produce invalid or unusable public APIs. A previous wider approach caused regressions in GLib and GObject because unions without sufficient infrastructure were exposed as public C# types.

## Public union wrapper

The purpose of a public union wrapper is:

- storing a native handle,
- providing a consistent managed entry point,
- enabling usage in callbacks and public method signatures,
- supporting GType-based integration when available.

In the current implementation, the public union wrapper maintains its own `IntPtr Handle`. It does not inherit from `GObject.Fundamental`, because that would be an overly strong and often incorrect assumption.

This is important because unions are generally not `GObject.Object` types and may not follow typical object lifecycle or instantiation rules.

## Internal and public sides

Unions have two distinct perspectives:

- Internal: P/Invoke, native calls, low-level representation.
- Public: C#-friendly wrapper and type conversion.

The internal side alone is not sufficient. Without a public wrapper, public delegates or method signatures can become impossible to generate.

This is exactly what happened with Clutter: the internal callback existed, but public callback generation failed due to missing union support.

## Callbacks and union

Without union support, callback rendering becomes problematic because the public C# parameter types must also be generated. If a callback expects a union, the generator must know:

- what the public C# parameter type should be,
- how to construct a managed wrapper from a native pointer,
- how to convert back if needed.

If this chain is incomplete, the callback delegate renderer throws, often resulting in empty generated files.

## Why not generate methods for every union?

Union method handling is more sensitive than for classes or records.

Generic public method and constructor renderers often assume that the callable `Parent` relationship is fully resolved and behaves like an object. During union iteration, it turned out this assumption does not hold for all union-related callables, leading to errors such as `Unknown parent`.

Because of this, the current implementation is deliberately conservative:

- the public union wrapper provides only the necessary structure,
- public union method generation is limited to safe cases,
- generic class/constructor logic is not blindly applied to all unions.

This is not a theoretical limitation but a defensive design choice based on the current generator architecture.

## The Parent issue (short version)

On the loader side, `Parent` is not passive data but a resolved relationship. If it is not resolved, accessing it throws an `Unknown parent` exception.

This matters because a faulty union rendering path triggered code that relied on `Parent`, even when it was not safe to do so.

This resulted in two separate issues:

- the root cause: incorrect union rendering path,
- a secondary issue: logging attempted to access `Parent.Name`, causing additional exceptions.

Logging was updated to use `CIdentifier ?? Name` to identify failing callables. This did not fix the root cause, but made diagnostics reliable.

## When is it correct to expose a union publicly?

Under the current rules, a union is exposed publicly only if it has sufficient type-level information to be safely integrated into the API. In practice, this currently means the presence of `TypeFunction`.

This balances two goals:

- avoid incomplete public APIs where unions are actually required,
- avoid generating misleading or unusable wrappers for unions lacking stable type information.

## Example: Clutter.Event

`Clutter.Event` is a clear example where union support is required:

- it is a union in GIR,
- it appears in public callbacks,
- it requires a managed wrapper,
- the callback path must construct it from an `IntPtr`.

Without this, the callback delegate cannot be generated correctly.

## Typical symptoms of missing union support

- empty or incomplete `.Generated.cs` files,
- missing public types in the generated API,
- callbacks not generated,
- `Unknown parent` exceptions during rendering,
- build errors that do not obviously point to unions.

## Important limitations

- The generator does not automatically clean obsolete generated files.
- If earlier iterations generated wrappers for more unions, and later rules restrict them, old generated files may remain.
- These stale files can cause misleading build errors.

Because of this, generator changes involving unions often require manual cleanup or a clean step before regeneration.

## Summary

Union handling in gir.core is not a minor detail. It directly affects:

- completeness of the public C# API,
- ability to generate callbacks,
- correctness of marshalling,
- overall robustness of the generator.

The goal is not to expose every union as a full public type, but to ensure that necessary and safely supported unions work reliably. `Clutter.Event` is a good example where lack of union support directly caused incorrect generation.

## Current scope

In the current state, this union implementation effectively affects only Clutter.

This is because among the available Linux GIR files, only `Clutter.Event` meets the criteria for public union wrapper generation, and it is the only one observed in the generated output.

Therefore, the current implementation primarily fixes `Clutter.Event` and the related callback and marshalling paths in Clutter. This is not a general statement about all future GIR states, but an observation based on the current repository.
