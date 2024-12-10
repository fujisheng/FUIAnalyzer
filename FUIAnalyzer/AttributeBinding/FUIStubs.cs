using System;

namespace FUI
{
    public class BindingAttribute : Attribute { }

    public class CommandAttribute : Attribute { }

    public interface IValueConverter<T1, T2> { }
}

namespace FUI.Bindable
{
    public class ObservableObject { }
    public class BindableProperty<T> { }
}