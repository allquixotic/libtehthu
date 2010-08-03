/* Copyright (c) 2010 Sean McNamara
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://sam.zoy.org/wtfpl/COPYING for more details. */

using System.Collections.Generic;
using System.Threading;

class SizeQueue<T>
{
    private readonly Queue<T> queue = new Queue<T>();
    private readonly int maxSize;
    public SizeQueue(int maxSize) { this.maxSize = maxSize; }

    public void Enqueue(T item)
    {
        lock (queue)
        {
            while (queue.Count >= maxSize)
            {
                Monitor.Wait(queue);
            }
            queue.Enqueue(item);
            if (queue.Count == 1)
            {
                // wake up any blocked dequeue
                Monitor.PulseAll(queue);
            }
        }
    }
    public T Dequeue()
    {
        lock (queue)
        {
            while (queue.Count == 0)
            {
                Monitor.Wait(queue);
            }
            T item = queue.Dequeue();
            if (queue.Count == maxSize - 1)
            {
                // wake up any blocked enqueue
                Monitor.PulseAll(queue);
            }
            return item;
        }
    }
}
