/*   LibTehthu -- a simple translator between syntactically-identical languages.
 *   Copyright (C) 2010  Sean McNamara
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>
 */

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
