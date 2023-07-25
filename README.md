# Session Based MudBlazor Authorization Template

<br>

A fork of [BlazorAuth by SymoHTL](https://github.com/SymoHTL/BlazorAuth)


## Customization
Modify the `AuthOptions.cs` file to change how long tokens should be valid.

```csharp
public static class AuthOptions {
    public static readonly TimeSpan TokenValidFor = TimeSpan.FromDays(30);
}
````