# CacheSample

This sample site shows how to use the cache.

### NuGet installation

Install the [Fody NuGet package](https://nuget.org/packages/Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```
PM> Install-Package Fody
```

### Add to FodyWeavers.xml

Add `<Cache/>` to [FodyWeavers.xml](https://github.com/Fody/Fody#add-fodyweaversxml) in the site project

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <Cache/>
</Weavers>
```

### Implement ICacheProvider interface

I have implemented two sample class in the CacheLib project. Use it free everywhere if you want. 


### Add CacheProvider in Startup.cs file

```
new CacheProvider(new RuntimeCache());
```

or you can just use the extention method of AddCache

```
services.AddCache(CacheType.InMemoryCache);
```

### Add [Cache] on your method like below
```
[Cache]
public static bool HasStock(int itemId)
```

or use the Duration feature

```
[Cache(Duration = 3600)]
public static decimal Calc(decimal a, decimal b)
```