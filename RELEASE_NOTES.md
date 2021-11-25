#### 4.0.0
* BREAKING: For VS2022+ now.
* BUGFIX: AD0001 -> Could not load file or assembly

#### 3.4.15
* BUGFIX: IDISP005 with ServiceDescriptor
* BUGFIX: IDISP004 when DisposeWith

#### 3.4.14
* BUGFIX: IDISP005 should not warn in Assert.Throws.
* BUGFIX: Handle function pointer.

#### 3.4.13
* BUGFIX: Specialcase Gu.Reactive extension methods.

#### 3.4.12
* BUGFIX: When leaveOpen has default value

#### 3.4.11
* BUGFIX IDISP023 handle trivial and.
* BUGFIX IDISP023 when chained constructors
* BUGFIX IDISP001 when if statement.
* BUGFIX IDISP004 when chained leave open.

#### 3.4.10
* BUGFIX: Handle using in loop

#### 3.4.9
* BUGFIX: IDISP023 Allow touching static reference types.
* BUGFIX: AD0001: Analyzer 'IDisposableAnalyzers.LocalDeclarationAnalyzer

#### 3.4.8
* BUGFIX: Don't use Roslyn's SymbolEqualityComparer

#### 3.4.7
* Can't repro issues, thinking maybe the 3.4.6 release used wrong binaries.

#### 3.4.6
* BUGFIX: IDSP007 when using declaration.
* BUGFIX: Figure out chained calls.

#### 3.4.5
* FEATURE: Handle switch expression.
* BUGFIX: Figure out await in more places.
* BUGFIX: Tweak assumptions about binary symbols.
* BUGFIX: Handle Interlocked.Exchange

#### 3.4.4
* FEATURE: Handle some common uses of reflection.

#### 3.4.3
* Special case ConnectionFactory.CreateConnection
* BUGFIX: Handle chained calls
* BUGFIX: Cast and dispose correctly.

#### 3.4.2
* Handle some regressions in Roslyn 3.7

#### 3.4.1
* Publish with binaries.

#### 3.4.0
* FEATURE: Handle DisposableMixins.DisposeWith
* BUGFIX: IDISP025 when abstract dispose method.
* BUGFIX: IDISP006 when explicit implementation.

#### 3.3.0
* FEATURE: Initial support for AsyncDisposable

#### 3.1.0
* BUGFIX IDISP005 when local function.
* BUGFIX IDISP024 don't call SuppressFinalize if sealed and no finalizer.
* BUGFIX IDISP025 seal disposable.

#### 2.1.2
* BUGFIX IDISP011: when disposing before foreach.
* BUGFIX IDISP003 should not warn when reassigning after dispose.
* BUGFIX IDISP002 & IDISP006 should not warn when assigned with created and injected.
* BUGFIX IDISP023: when disposing members.

#### 2.1.1
* BUGFIX: File.ReadAllText does not create a disposable.

#### 2.1
* New analyzer: IDISP018.
* New analyzer: IDISP019.
* New analyzer: IDISP020.
* New analyzer: IDISP021.
* New analyzer: IDISP022.
* New analyzer: IDISP023.

#### 2.0.7
* BUGFIX: IDISP003 in loops.
* BUGFIX: Generate SuppressFinalize when virtual.

#### 2.0.6
* BUGFIX: IDISP003 figure out when assigned in switch.
* IDISP003 should not warn when assigning out parameter in if return.

#### 2.0.5
* IDISP004 warn on explicit discard.
* IDISP017 when disposing in finally.
* IDISP003 when assigning in loop.

#### 2.0.4
* BUGFIX: IDISP003 detect disposal of previous instance copied to local.
* BUGFIX: IDISP015 when returning from value from dictionary.
* FEATURE: Understand NOP disposable.

#### 2.0.3.3
* BUGFIX: IDISP013 ignore NUnit ThrowAsync
* BUGFIX: Code fix for IDISP001 doubles the indentation
* BUGFIX: Code fix for IDISP002 when calling base.Dispose(disposing)
* BUGFIX: IDISP006 should not warn when overriding Dispose(disposing)

#### 2.0.3
* BUGFIX: Handle extension methods in binary references better.

#### 2.0.1
* BUGFIX: INPC013 return early & return null.
* FEATURE: New analyzers & fixes.

#### 2.0.1
* BUGFIX: Handle recursion.

#### 2.0.0
* FEATURE: Support C#7.

#### 1.0.0
* Bugfixes

#### 0.1.4.4
* FEATÙRE: IDISP014 check that HttpClient is assigned to static field or property.
* BUGFIX: IDISP001 handle dispose in lambda closure.
* BUGFIX: IDISP004 handle properties.

#### 0.1.4.4
* BUGFIX: IDISP011 handle recursion.
* BUGFIX: IDISP011 handle yield return.
* BUGFIX: IDISP003 handle lambda.

#### 0.1.4.3
* BUGFIX: IDISP004 handle CompositeDisposable.Add.

#### 0.1.4
* PERFORMANCE: Merged many analyzers doing the same expensive analysis to fewer.
* BUGFIX: IDISP004 nag on argument to invocation.
* BUGFIX: IDISP003 handle lazy properties.
* BUGFIX: Generate correct code for disposing explicit disposable.
* BUGFIX: IDISP004 nag on chained invocation.
* BUGFIX: IDISP004 default warning.
* FEATURE: IDISP0011 don't return disposed instance.
* FEATURE: IDISP0011 property should not return created disposable.

#### 0.1.3.1
* BUGFIX: Don't nag about implementing IDisposable if disposing in teardown
* FEATURE: Codefix suggesting generate dispose in teardown for dispose before reassigning.

#### 0.1.3
* BUGFIX: Handle dispose in setter.
* FEATURE: Handle Setup & TearDown

#### 0.1.2
* BUGFIXES: whitespace in codegen.
* BUGFIX: Override public virtual Dispose