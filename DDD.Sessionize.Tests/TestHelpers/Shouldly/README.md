![Shouldly Logo](https://raw.githubusercontent.com/shouldly/shouldly/master/assets/logo_350x84.png)  
========

See https://github.com/shouldly/shouldly

# .NET Core ShouldMatchApproved

**NB.** The `InFolder` extension method is how I worked around the missing `StackTrace` APIs in `netcore1`. It traverses up 3 directory levels to find the root of the test project. e.g. `var srcFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));`

### Usage

```cs
response.ShouldMatchApproved(options =>
    options.WithName("PrefixWith")
        .WithScrubber(ReplaceGeneratedIds)
        .WithDescriminator($"_{testName}")
        .WithFileExtension("json")
        .InFolder(nameof(MyNamespace), "Approved"));
```

#### Basic GUID Scrubber

```
string ReplaceGeneratedIds(string received)
{
    return Regex.Replace(received, "[{(\"]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[\")}]?", $"\"{Guid.Empty:D}\"", RegexOptions.IgnoreCase);
}
```

## Currently maintained by
 - [Jake Ginnivan](https://github.com/JakeGinnivan)
 - [Joseph Woodward](https://github.com/JosephWoodward)

If you are interested in helping out, jump on [gitter](https://gitter.im/shouldly/shouldly) and have a chat.

## Brought to you by
 - Dave Newman
 - Xerxes Battiwalla
 - Anthony Egerton
 - Peter van der Woude
 - Jake Ginnivan
