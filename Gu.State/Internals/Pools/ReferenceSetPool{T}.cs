﻿namespace Gu.State
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal static class ReferenceSetPool<T>
        where T : class
    {
        private static readonly ConcurrentQueue<HashSet<T>> Pool = new ConcurrentQueue<HashSet<T>>();

        internal static IBorrowed<HashSet<T>> Borrow()
        {
            HashSet<T> set;
            if (Pool.TryDequeue(out set))
            {
                return Borrowed.Create(set, Return);
            }
            else
            {
                return Borrowed.Create(new HashSet<T>(ReferenceComparer<T>.Default), Return);
            }
        }

        private static void Return(HashSet<T> set)
        {
            set.Clear();
            Pool.Enqueue(set);
        }
    }
}