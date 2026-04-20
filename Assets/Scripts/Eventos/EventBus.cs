using System;
using System.Collections.Generic;

public interface IEvent { }

public static class EventBus
{
   // Para que no pete si vienen dos a la vez 
    private static readonly object _lock = new object(); 
    //Guardamos el evento y la acción que hace
    private static readonly Dictionary<Type, Action<IEvent>> events = new Dictionary<Type, Action<IEvent>>(); 
    //No guardamos la referecia porque por función entra un genérico y no se puede saber cual es.
    private static readonly Dictionary<(Delegate, Type), Action<IEvent>> references = new Dictionary<(Delegate, Type), Action<IEvent>>(); 

    public static void Subscribe<T>(Action<T> listener) where T : IEvent
    {
        lock (_lock)
        {
            var key = ((Delegate)listener, typeof(T));
            if (references.ContainsKey(key)) return; // Evitamos suscripciones duplicadas

            // Pasamos de genérico a no genérico para guardarlo en el diccionario
            Action<IEvent> wrapper = (e) => listener((T)e);
            references[key] = wrapper;

            Type type = typeof(T);
            if (!events.TryGetValue(type, out var existing)) //Si no existe lo creamos
                events[type] = wrapper;
            else
                events[type] = existing + wrapper; //Si ya existe, añadimos nueva función.
        }
    }
    public static void Clear()
    {
        lock (_lock)
        {
            events.Clear();
            references.Clear();
        }
    }

    public static void Unsubscribe<T>(Action<T> listener) where T : IEvent
    {
        lock (_lock)
        {
            var key = ((Delegate)listener, typeof(T));
            // Busca en las refrenecias el evento que toca eliminar
            if (references.TryGetValue(key, out Action<IEvent> wrapper))
            {
                Type type = typeof(T);
                if (events.ContainsKey(type))
                {
                    //Quita la función del evento
                    events[type] -= wrapper;
                    //Si no quedan funciones, quitamos el evento
                    if (events[type] == null)
                        events.Remove(type);
                }
                // Se borra la referencia
                references.Remove(key);
            }
        }
    }

    public static void Publish<T>(T eventItem) where T : IEvent
    {
        Action<IEvent> action;
        lock (_lock)
        {
            // Busca listeners para el evento
            events.TryGetValue(typeof(T), out action);
        }
        // Si hay listeners los llama, pasándoles los parámetros
        action?.Invoke(eventItem);
    }
}