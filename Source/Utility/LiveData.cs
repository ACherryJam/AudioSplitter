using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.AudioSplitter.Utility
{
    public class LiveData<T>
    {
        public delegate void PropertyChangedEvent(T newValue);

        public event PropertyChangedEvent PropertyChanged;

        private T value;
        public T Value
        {
            get { return value; }
            set
            {
                this.value = value;
                Notify();
            }
        }

        public void Notify()
        {
            PropertyChanged?.Invoke(Value);
        }

        public void Observe(Action<T> observer)
        {
            PropertyChanged += observer.Invoke;
            observer.Invoke(Value);
        }

        public void StopObserving(Action<T> observer)
        {
            PropertyChanged -= observer.Invoke;
        }

        public LiveData() { }
        public LiveData(T value) { Value = value; }
    }
}
