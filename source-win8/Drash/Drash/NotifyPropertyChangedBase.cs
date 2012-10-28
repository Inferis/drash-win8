using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using iPASoftware.iRAD.Basics.Extensions;

namespace Drash
{
    /// <summary>
    /// no comment.
    /// Base class for INotifyPropertyChanged implementation.
    /// </summary>
    [DataContract]
    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged<T>(Expression<Func<T>> prop)
        {
            OnPropertyChanged(prop.GetMemberInfo().Name);
        }

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        //public static PropertyChangedEventHandler AutoPropagate(INotifyPropertyChanged x)
        //{
        //    return (s, e) => x.NotifyPropertyChanged(e.PropertyName);
        //}
    }
}