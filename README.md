![Logo](Art/Logo150x150.png "Logo")

# Genesis.AsyncInitializationGuard

[![Build status](https://ci.appveyor.com/api/projects/status/x6se35kyjcl5u2p9?svg=true)](https://ci.appveyor.com/project/kentcb/genesis-asyncinitializationguard)

## What?

> All Genesis.* projects are formalizations of small pieces of functionality I find myself copying from project to project. Some are small to the point of triviality, but are time-savers nonetheless. They have a particular focus on performance with respect to mobile development, but are certainly applicable outside this domain.
 
**Genesis.AsyncInitializationGuard** provides an `InitializationGuard` class that facilitates asynchronous, thread-safe initialization. It is delivered as a netstandard 1.0 binary.

## Why?

Sometimes you have a component that needs to be initialized, and that initialization requires asynchronous calls. For example, perhaps you need to obtain some settings from the database before the component can be considered initialized. This can result in nasty bugs where attempting to initialize more than once in rapid succession executes the initialization logic more than once.

The `InitializationGuard` class provided by **Genesis.AsyncInitializationGuard** wraps a piece of asynchronous initialization logic and guarantees that it will only execute it to completion once.

## Where?

The easiest way to get **Genesis.AsyncInitializationGuard** is via [NuGet](http://www.nuget.org/packages/Genesis.AsyncInitializationGuard/):

```PowerShell
Install-Package Genesis.AsyncInitializationGuard
```

## How?

**Genesis.AsyncInitializationGuard** provides the `InitializationGuard` class, which can be used as follows:

```C#
public class SomeClass
{
    private readonly Database database;
    private readonly InitializationGuard guard;
    private int someSetting;

    public SomeClass(Database database)
    {
        this.database = database;
        this.guard = new InitializationGuard(this.InitializeCore);
    }

    // any number of threads can call this any number of times...
    public IObservable<Unit> Initialize() => this.guard.Initialize();

    // ...but this will only execute to completion once
    private IObservable<Unit> InitializeCore() =>
        database
            .GetSetting("foo")
            .Do(setting => this.someSetting = setting)
            .Select(_ => Unit.Default);
}
``` 

## Who?

**Genesis.AsyncInitializationGuard** is created and maintained by [Kent Boogaart](http://kent-boogaart.com). Issues and pull requests are welcome.