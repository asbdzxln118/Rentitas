﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Rentitas
{
    public partial class Entity
    {

        // This method is used internally. Don't call it yourself.
        // Use pool.DestroyEntity(entity);
        internal void Destroy()
        {
            RemoveAllComponents();
            OnComponentAdded = null;
            OnComponentReplaced = null;
            OnComponentRemoved = null;
            _isEnabled = false;
        }

        // Do not call this method manually. This method is called by the pool.
        internal void RemoveAllOnEntityReleasedHandlers()
        {
            OnEntityReleased = null;
        }



        /// Removes all components.
        public void RemoveAllComponents()
        {
            _toStringCache = null;
            foreach (var key in _poolMeta.ComponentTypes)
            {
                ReplaceInstance(key, null);
            }
        }

        /// Returns the componentPool for the specified component index.
        /// componentPools is set by the pool which created the entity and is used to reuse removed components.
        /// Removed components will be pushed to the componentPool.
        /// Use entity.CreateComponent(index, type) to get a new or reusable component from the componentPool.
        public Stack<IComponent> GetComponentPool(Type type)
        {
            return ComponentsPool[type];
        }


        private bool Has(Type type)
        {
            return _components[type] != null;
        }

        public bool Has(params Type[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (!Has(types[i])) return false;
            }

            return true;
        }

        public bool HasAny(params Type[] anyOfTypes)
        {
            for (int i = 0; i < anyOfTypes.Length; i++)
            {
                if (Has(anyOfTypes[i]))
                    return true;
            }

            return false;
        }

        public T CreateComponent<T>() where T : IComponent, new()
        {
            var pool = GetComponentPool(typeof (T));

            if (pool.Count > 0)
                return (T) pool.Pop();

            return new T();
        }

        public Entity AddInstance<T>(T component) where T : IComponent
        {
            if (!_isEnabled)
            {
                throw new EntityIsNotEnabledException("Cannot add component '" + component + "' to " + this + "!");
            }

            var type = component.GetType();

            if (Has(type))
            {
                throw new EntityAlreadyHasComponentException(
                    type,
                    "Cannot add component '" + component + "' to " + this + "!",
                    "You should check if an entity already has the component before adding it or use entity.ReplaceComponent()."
                );
            }

            _components[type] = component;
            _componentTypesCache = null;
            _componentsCache = null;
            _toStringCache = null;

            OnComponentAdded?.Invoke(this, type, component);

            return this;
        }

        private Entity Remove(Type type)
        {
            if (!_isEnabled)
            {
                throw new EntityIsNotEnabledException("Cannot remove component '" + type + "' from " + this + "!");
            }

            if (!Has(type))
            {
                throw new EntityDoesNotHaveComponentException(
                    type,
                    "Cannot remove component '" + type + "' from " + this + "!",
                    "You should check if an entity has the component before removing it."
                );
            }


            return ReplaceInstance(type, null);
        }

        private Entity ReplaceInstance(Type type, IComponent component)
        {
            if (!_isEnabled)
            {
                throw new EntityIsNotEnabledException("Cannot replace component '" + component + "' on " + this + "!");
            }

            if (Has(type))
            {
                var previousComponent = _components[type];
                if (component != previousComponent)
                {
                    _components[type] = component;
                    _componentsCache = null;
                    if (component != null)
                    {
                        OnComponentReplaced?.Invoke(this, type, previousComponent, component);
                    }
                    else
                    {
                        _componentTypesCache = null;
                        OnComponentRemoved?.Invoke(this, type, previousComponent);
                    }

                    GetComponentPool(type).Push(previousComponent);

                }
                else
                {
                    OnComponentReplaced?.Invoke(this, type, previousComponent, component);
                }
            }
            else if (component != null)
            {
                AddInstance(component);
            }

            return this;
        }
    }
}