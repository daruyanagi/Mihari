using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

abstract public class BindableBase : INotifyPropertyChanged
{
    protected BindableBase() { }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
        var handler = PropertyChanged;
        if (handler != null)
        {
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
    {
        if (object.Equals(storage, value)) return false;
        storage = value;
        this.RaisePropertyChanged(propertyName);
        return true;
    }
}

