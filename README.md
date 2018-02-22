# Cache

<img src="https://github.com/KevinYeti/Cache/raw/master/icon.png" width="64">

Injects method cache code.

### This is an add-in for [Fody](https://github.com/Fody/Fody/)
[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage)

This project was forked by [MethodCache](https://github.com/Dresel/MethodCache) original.
And updated to support DotNet Core.

## Milestone
- [x] Support dotnet core
- [x] Support instance of class cache
- [x] Support static of method cache
- [ ] Support property of class cache
- [ ] Support complex parameters of method cache
- [x] Support duration control

## Your Code

	[Cache]
	public int Add(int a, int b)
	{
		return a + b;
	}

	[Cache]
	public string AlsoWorksForProperties
	{
		get
		{
			return DoSomeCalculations(this.parameterField);
		}
		set
		{
			this.parameterField = value;
		}
	}

## What gets compiled

	public int Add(int a, int b)
	{
		string cacheKey = string.Format("Namespace.Class.Add_{0}_{1}", new object[] { a, b });
	
		if(Cache.Contains(cacheKey))
		{
			return Cache.Retrieve<int>(cacheKey);
		}
		
		int result = a + b;
		
		Cache.Store(cacheKey, result);
		
		return result;
	}
  

## Usage

See also [Fody usage](https://github.com/Fody/Fody#usage).


### NuGet installation

Install the [Cache.Fody NuGet package](https://nuget.org/packages/Cache.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```
PM> Install-Package Cache.Fody
PM> Update-Package Fody
```

The `Update-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<Cache/>` to [FodyWeavers.xml](https://github.com/Fody/Fody#add-fodyweaversxml)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <Cache/>
</Weavers>
```

## Whats in the NuGet

In addition to the actual weaving assembly the NuGet package will also add a file `CacheAttribute.cs` to the target project.

```
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor,AllowMultiple = false)]
class CacheAttribute : Attribute
{
}
```

## Icon

Icon courtesy of [The Noun Project](http://thenounproject.com)
