using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.AudioSplitter.Utility
{
    public class LiveData<T> where T : notnull
    {
        public delegate void PropertyChangedEvent(T? newValue);

        public event PropertyChangedEvent PropertyChanged = _ => { };

        private T? value;
        public T? Value
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

        public void Observe(PropertyChangedEvent observer)
        {
            PropertyChanged += observer.Invoke;
            observer.Invoke(Value);
        }

        public void StopObserving(PropertyChangedEvent observer)
        {
            PropertyChanged -= observer.Invoke;
        }

        public LiveData() { }
        public LiveData(T value) { Value = value; }
    }
}
