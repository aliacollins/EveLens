using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace EVEMon.Common.ViewModels.Binding
{
    /// <summary>
    /// Extension methods for one-way ViewModel-to-Control property binding.
    /// All bindings auto-marshal to the UI thread via <see cref="Control.BeginInvoke(Delegate)"/>.
    /// Returns <see cref="IDisposable"/> for cleanup (Law #11).
    /// </summary>
    public static class PropertyBinding
    {
        /// <summary>
        /// Binds a ViewModel property to a control's <see cref="Control.Text"/> property.
        /// Updates immediately with current value, then on every PropertyChanged.
        /// </summary>
        /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
        /// <param name="vm">The ViewModel instance.</param>
        /// <param name="control">The target control.</param>
        /// <param name="propertyName">The ViewModel property name to observe.</param>
        /// <param name="valueSelector">Function to extract the text value from the ViewModel.</param>
        /// <returns>An <see cref="IDisposable"/> that removes the binding when disposed.</returns>
        public static IDisposable BindText<TViewModel>(
            this TViewModel vm,
            Control control,
            string propertyName,
            Func<TViewModel, string> valueSelector)
            where TViewModel : INotifyPropertyChanged
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

            return Bind(vm, control, propertyName, valueSelector, (c, v) => c.Text = v);
        }

        /// <summary>
        /// Binds a ViewModel property to a control's <see cref="Control.Visible"/> property.
        /// </summary>
        public static IDisposable BindVisible<TViewModel>(
            this TViewModel vm,
            Control control,
            string propertyName,
            Func<TViewModel, bool> valueSelector)
            where TViewModel : INotifyPropertyChanged
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

            return Bind(vm, control, propertyName, valueSelector, (c, v) => c.Visible = v);
        }

        /// <summary>
        /// Generic one-way binding from a ViewModel property to a control.
        /// Applies the current value immediately, then listens for PropertyChanged.
        /// </summary>
        /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
        /// <typeparam name="TControl">The control type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="vm">The ViewModel instance.</param>
        /// <param name="control">The target control.</param>
        /// <param name="propertyName">The ViewModel property name to observe.</param>
        /// <param name="valueSelector">Function to extract the value from the ViewModel.</param>
        /// <param name="applier">Action to apply the value to the control.</param>
        /// <returns>An <see cref="IDisposable"/> that removes the binding when disposed.</returns>
        public static IDisposable Bind<TViewModel, TControl, TValue>(
            this TViewModel vm,
            TControl control,
            string propertyName,
            Func<TViewModel, TValue> valueSelector,
            Action<TControl, TValue> applier)
            where TViewModel : INotifyPropertyChanged
            where TControl : Control
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
            if (applier == null) throw new ArgumentNullException(nameof(applier));

            // Apply current value immediately
            ApplyValue(vm, control, valueSelector, applier);

            // Subscribe to changes
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName != propertyName && !string.IsNullOrEmpty(e.PropertyName))
                    return;

                if (control.IsDisposed)
                    return;

                if (control.InvokeRequired)
                {
                    try
                    {
                        control.BeginInvoke(new Action(() =>
                        {
                            if (!control.IsDisposed)
                                ApplyValue(vm, control, valueSelector, applier);
                        }));
                    }
                    catch (ObjectDisposedException)
                    {
                        // Control was disposed between check and invoke
                    }
                    catch (InvalidOperationException)
                    {
                        // Handle not created yet or already destroyed
                    }
                }
                else
                {
                    ApplyValue(vm, control, valueSelector, applier);
                }
            };

            vm.PropertyChanged += handler;

            return new ActionDisposable(() => vm.PropertyChanged -= handler);
        }

        private static void ApplyValue<TViewModel, TControl, TValue>(
            TViewModel vm,
            TControl control,
            Func<TViewModel, TValue> valueSelector,
            Action<TControl, TValue> applier)
            where TControl : Control
        {
            try
            {
                applier(control, valueSelector(vm));
            }
            catch (ObjectDisposedException)
            {
                // Control was disposed
            }
        }
    }
}
