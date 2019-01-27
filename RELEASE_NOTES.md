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