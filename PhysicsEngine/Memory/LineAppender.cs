using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace PhysicsEngine.Memory;

public struct LineAppender(StringBuilder builder, IFormatProvider? provider)
{
    public readonly StringBuilder Builder => builder;
    public readonly IFormatProvider? Provider => provider;

    private bool _pending;

    public void AppendLine([InterpolatedStringHandlerArgument("")] ref Handler handler)
    {
        _pending = true;
    }

    public void Flush()
    {
        if (_pending)
        {
            builder.AppendLine();
            _pending = false;
        }
    }

    [InterpolatedStringHandler]
    public struct Handler
    {
        private StringBuilder.AppendInterpolatedStringHandler _handler;

        public Handler(int literalLength, int formattedCount, LineAppender appender)
        {
            appender.Flush();
            _handler = new(literalLength, formattedCount, appender.Builder, appender.Provider);
        }

        public void AppendLiteral(string value) =>
            _handler.AppendLiteral(value);

        public void AppendFormatted<T>(T value) =>
            _handler.AppendFormatted(value);

        public void AppendFormatted<T>(T value, string? format) =>
            _handler.AppendFormatted(value, format);

        public void AppendFormatted<T>(T value, int alignment) =>
            _handler.AppendFormatted(value, alignment, format: null);

        public void AppendFormatted<T>(T value, int alignment, string? format) =>
            _handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(ReadOnlySpan<char> value) =>
            _handler.AppendFormatted(value);

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) =>
            _handler.AppendFormatted(value, alignment, format);
    }
}
