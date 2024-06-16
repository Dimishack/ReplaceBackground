using System.Windows.Input;

namespace ReplaceBackground.Infrastructure.Commands.Base
{
    abstract class Command : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        #region Executable : bool - Разрешение на использование команды

        ///<summary>Разрешение на использование команды</summary>
        private bool _executable = true;

        ///<summary>Разрешение на использование команды</summary>
        public bool Executable
        {
            get => _executable;
            set
            {
                if (Equals(value, _executable)) return;
                _executable = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        bool ICommand.CanExecute(object? parameter) => CanExecute(parameter) && _executable;

        void ICommand.Execute(object? parameter)
        {
            if(CanExecute(parameter))
                Execute(parameter);
        }

        protected virtual bool CanExecute(object? parameter) => true;

        protected abstract void Execute(object? parameter);
    }
}
