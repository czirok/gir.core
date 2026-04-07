# Analysis

## In short

The vast majority of the errors trace back to the same generator issue:

- the Gck GIR actually defines a property named `handle`,
- the public property generator blindly emits it as `Handle`,
- this shadows the inherited native `Handle` member,
- while the public method generator still assumes that `.Handle` is a native safe handle on which `DangerousGetHandle()` can be called.

As a result, the compiler no longer sees the native handle object behind `.Handle`, but the generated `ulong Handle` property, leading to repeated errors like:

- `ulong` does not contain a definition for `DangerousGetHandle`

## Concrete evidence in the generated code

The following public files contain an actual `Handle` property of type `ulong`:

- [`src/Libs/Gck-2/Public/Object.Properties.Generated.cs`](/src/Libs/Gck-2/Public/Object.Properties.Generated.cs)
- [`src/Libs/Gck-2/Public/Session.Properties.Generated.cs`](/src/Libs/Gck-2/Public/Session.Properties.Generated.cs)
- [`src/Libs/Gck-2/Public/Slot.Properties.Generated.cs`](/src/Libs/Gck-2/Public/Slot.Properties.Generated.cs)

Example:

- `public ulong Handle`

This property is not the native object handle, but a PKCS#11 identifier accessed through the GObject property system.

Meanwhile, the public methods and parameter converters generate code like:

- `slot.Handle.DangerousGetHandle()`
- `session.Handle.DangerousGetHandle()`
- `key.Handle.DangerousGetHandle()`
- `wrapper.Handle.DangerousGetHandle()`
- `this.Handle.DangerousGetHandle()`

Once `Handle` resolves to a `ulong` property, all of these become invalid.

## The issue does not originate from incorrect GIR data

The GIR genuinely defines these properties:

- `Gck.Object.handle`
- `Gck.Session.handle`
- `Gck.Slot.handle`

All of them are of type `gulong`, meaning the generator is working from valid introspection data.

So the problem is not that GIR is wrong, but that the public naming and collision-handling logic does not account for inherited `Handle` members.

## Where exactly the generator fails

### 1. The property name is blindly turned into `Handle`

File:

- [`src/Generation/Generator/Model/Property.cs`](/src/Generation/Generator/Model/Property.cs)

Here the property name is simply converted to PascalCase:

- `handle` -> `Handle`

There is no safeguard to check whether this name collides with an inherited member.

### 2. Existing fixers do not cover this case

Existing fixers:

- [`src/Generation/Generator/Fixer/Class/PropertyNamedLikeClassFixer.cs`](/src/Generation/Generator/Fixer/Class/PropertyNamedLikeClassFixer.cs)
- [`src/Generation/Generator/Fixer/Class/PublicMethodsColldingWithPropertiesFixer.cs`](/src/Generation/Generator/Fixer/Class/PublicMethodsColldingWithPropertiesFixer.cs)
- [`src/Generation/Generator/Fixer/Class/PropertyLikeInterfacePropertyFixer.cs`](/src/Generation/Generator/Fixer/Class/PropertyLikeInterfacePropertyFixer.cs)

These handle certain naming conflicts, but not the case where a property name collides with an inherited base class member.

In other words, collision handling exists, just not for this scenario.

### 3. Public parameter and instance converters assume a safe handle

Files:

- [`src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Class.cs`](/src/Generation/Generator/Renderer/Public/ParameterToNativeExpression/Converter/Class.cs)
- [`src/Generation/Generator/Renderer/Public/InstanceParameterToNativeExpression/Converter/Class.cs`](/src/Generation/Generator/Renderer/Public/InstanceParameterToNativeExpression/Converter/Class.cs)

These parts of the generator assume that `.Handle` on a class instance is a native handle object, and generate code like:

- `parameter.Handle.DangerousGetHandle()`
- `this.Handle.DangerousGetHandle()`
- `base.Handle.DangerousGetHandle()`

This assumption would be correct if the `Handle` name were not shadowed by a `ulong` property in the public API.

## Why does this produce so many errors at once?

Because the same naming collision affects multiple generated public files:

- static factory and helper methods
- instance methods
- interface implementations
- derived classes such as `ObjectCacheHelper`

So the same compilation pattern appears in many places, but the root cause is a single shared generator issue.

## The secondary issue in `Object.Framework.Generated.cs`

This file not only shows the `Handle` collision, but also a separate local variable naming conflict:

- the method `FromHandle(Gck.Session session, ulong objectHandle)` has a parameter named `objectHandle`
- the generator also tries to declare a local variable with the same name:
  - `var objectHandle = ...`

This results in:

- `CS0136`
- `CS0841`

This is not the same issue as the `DangerousGetHandle` problem, but it appears in the same generated area.

## Conclusion

In the Gck case, the core generator issue is:

- public property generation creates a `ulong Handle` property,
- while public method generation still expects the inherited native handle under the same name.

So two parts of the generator contradict each other:

1. one exposes a public PKCS#11 identifier as `Handle`,
2. the other expects a native safe handle under the same name.

This makes the generated code internally inconsistent at the type system level.

## What generator fix is needed?

The fix that best aligns with the generator philosophy is not modifying the GIR, but handling the conflicting property name in the generator.

Required direction:

- detect during property generation when `Handle` collides with an inherited member,
- in such cases, rename the property or exclude it from the public surface,
- and ensure the property descriptor generation follows the same adjusted name.

In short: the core issue is not the `ulong` conversion, but the `Handle` name collision in the public API.
