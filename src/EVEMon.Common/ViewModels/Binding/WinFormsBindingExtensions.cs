using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace EVEMon.Common.ViewModels.Binding
{
    /// <summary>
    /// Additional WinForms-specific binding helpers beyond <see cref="PropertyBinding"/>.
    /// </summary>
    public static class WinFormsBindingExtensions
    {
        /// <summary>
        /// Binds a ViewModel property to a control's <see cref="Control.Enabled"/> property.
        /// </summary>
        public static IDisposable BindEnabled<TViewModel>(
            this TViewModel vm,
            Control control,
            string propertyName,
            Func<TViewModel, bool> valueSelector)
            where TViewModel : INotifyPropertyChanged
        {
            return PropertyBinding.Bind(vm, control, propertyName, valueSelector,
                (c, v) => c.Enabled = v);
        }

        /// <summary>
        /// Binds a ViewModel property to a <see cref="ToolStripStatusLabel.Text"/> property.
        /// ToolStripItems are not Controls so they require special handling.
        /// </summary>
        public static IDisposable BindToolStripLabel<TViewModel>(
            this TViewModel vm,
            ToolStripStatusLabel label,
            string propertyName,
            Func<TViewModel, string> valueSelector)
            where TViewModel : INotifyPropertyChanged
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

            // Apply current value
            label.Text = valueSelector(vm);

            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName != propertyName && !string.IsNullOrEmpty(e.PropertyName))
                    return;

                var owner = label.Owner;
                if (owner == null || owner.IsDisposed)
                    return;

                if (owner.InvokeRequired)
                {
                    try
                    {
                        owner.BeginInvoke(new Action(() =>
                        {
                            if (!owner.IsDisposed)
                                label.Text = valueSelector(vm);
                        }));
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
                else
                {
                    label.Text = valueSelector(vm);
                }
            };

            vm.PropertyChanged += handler;
            return new ActionDisposable(() => vm.PropertyChanged -= handler);
        }
    }
}
