namespace Grekov.Abstractions.Interfaces.Definitions;

public interface IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType);

	public object? Convert(DefValue value, Type targetType, IDefConverter converter);
}

public interface IDefConverter
{
	public object? Convert(DefValue value, Type targetType);
}