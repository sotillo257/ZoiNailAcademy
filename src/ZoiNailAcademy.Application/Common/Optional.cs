namespace ZoiNailAcademy.Application.Common;

public readonly struct Optional<T>
{
    public bool HasValue { get; }
    public T? Value { get; }

    private Optional(bool hasValue, T? value)
    {
        HasValue = hasValue;
        Value = value;
    }

    public static Optional<T> Some(T? value) => new(true, value);
    public static Optional<T> None() => default;
}
