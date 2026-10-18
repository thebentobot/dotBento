namespace dotBento.Bot.Attributes;

/// <summary>
/// Marks a command or component as available even when the invoking user has opted out of data
/// storage. Used by <c>/user settings</c> so an opted-out user can opt back in.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class OptOutExempt : Attribute;
