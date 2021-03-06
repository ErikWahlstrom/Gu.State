﻿// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Gu.State.Benchmarks
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    public class ComplexType : INotifyPropertyChanged
    {
        private string name;
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public ComplexType()
        {
        }

        public ComplexType(string name, int value)
        {
            this.name = name;
            this.value = value;
        }

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (value == this.name) return;
                this.name = value;
                this.OnPropertyChanged();
            }
        }

        public int Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if (value == this.value) return;
                this.value = value;
                this.OnPropertyChanged();
            }
        }

        public static bool operator ==(ComplexType left, ComplexType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ComplexType left, ComplexType right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return this.Equals((ComplexType)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Name?.GetHashCode() ?? 0) * 397) ^ this.Value;
            }
        }

        protected bool Equals(ComplexType other)
        {
            return string.Equals(this.Name, other.Name) && this.Value == other.Value;
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}