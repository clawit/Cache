# Cache

## This is an add-in for [Fody](https://github.com/Fody/Fody/)

<img src="https://github.com/KevinYeti/Cache/raw/master/icon.png" width="64">

Injects method cache code.

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage)

## Milestone
- [x] Support instance of class cache
- [x] Support static of method cache
- [ ] Support property of class cache
- [ ] Support complex parameters of method cache

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