using System;
using System.Windows.Input;

namespace Drash.Models
{
    public class ActionCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public ActionCommand(Action action) : this(o => action())
        {
        }

        public ActionCommand(Action action, Func<bool> canExecute) : this(o => action(), o => canExecute())
        {
        }

        public ActionCommand(Action<object> action) : this(action, o => true) 
        {
        }

        public ActionCommand(Action<object> action, Func<object, bool> canExecute)
        {
            this.execute = action;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            CanExecuteChanged(this, new EventArgs());
            execute(parameter);
            CanExecuteChanged(this, new EventArgs());
        }

        public event EventHandler CanExecuteChanged = delegate { };
    }
}