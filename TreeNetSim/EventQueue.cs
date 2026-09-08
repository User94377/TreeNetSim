using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Generic;
using System.Linq;

namespace TreeNetSim
{
    public class EventQueue
    {
        private List<SimEvent> events;
        private int nextId = 0;

        public EventQueue()
        {
            events = new List<SimEvent>();
        }

        // Добавить событие в очередь с сохранением сортировки по времени
        public void Enqueue(SimEvent simEvent)
        {
            simEvent.Id = nextId++;  

            int index = events.BinarySearch(simEvent, Comparer<SimEvent>.Create((a, b) =>
            {
                int cmp = a.Time.CompareTo(b.Time);
                return cmp != 0 ? cmp : a.Id.CompareTo(b.Id);
            }));

            if (index < 0) index = ~index;
            events.Insert(index, simEvent);
        }

        // Извлечь событие с наименьшим временем
        public SimEvent Dequeue()
        {
            if (events.Count > 0)
            {
                SimEvent firstEvent = events[0];
                events.RemoveAt(0);
                return firstEvent;
            }
            return null;
        }

        // Проверка на пустоту
        public bool IsEmpty
        {
            get { return events.Count == 0; }
        }

        // Количество событий в очереди
        public int Count
        {
            get { return events.Count; }
        }

        // Очистить очередь
        public void Clear()
        {
            events.Clear();
            nextId = 0; 
        }

        // Получить следующее событие без извлечения
        public SimEvent Peek()
        {
            if (events.Count > 0)
            {
                return events[0];
            }
            return null;
        }
    }
}
