using ReplaceBackground.Infrastructure.Commands.Base;
using System.Diagnostics.CodeAnalysis;

namespace ReplaceBackground.Infrastructure.Commands
{
    class LambdaCommand([NotNull] Action<object?> execute, Func<object?, bool>? canExecute = null) : Command
    {
        private readonly Action<object?> _execute = execute;
        private readonly Func<object?, bool>? _canExecute = canExecute;


        protected override bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        protected override void Execute(object? parameter)
        {
            if(CanExecute(parameter))
                _execute(parameter);
        }
    }
}
