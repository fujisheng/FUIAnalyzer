using System;

namespace FUI
{
    public class BindingAttribute : Attribute { }

    public interface IValueConverter<T1, T2> { }
}

namespace FUI.Bindable
{
    public class BindableProperty<T> { }
}