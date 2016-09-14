// ==========================================================================
//  Schema.cs
//  PinkParrot Headless CMS
// ==========================================================================
//  Copyright (c) PinkParrot Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using PinkParrot.Infrastructure;

// ReSharper disable InvertIf

namespace PinkParrot.Core.Schemas
{
    public sealed class Schema
    {
        private readonly string name;
        private readonly SchemaProperties properties;
        private readonly ImmutableDictionary<long, Field> fieldsById;
        private readonly Dictionary<string, Field> fieldsByName;

        public string Name
        {
            get { return name; }
        }

        public ImmutableDictionary<long, Field> Fields
        {
            get { return fieldsById; }
        }

        public SchemaProperties Properties
        {
            get { return properties; }
        }

        public Schema(string name, SchemaProperties properties, ImmutableDictionary<long, Field> fields)
        {
            Guard.NotNull(fields, nameof(fields));
            Guard.NotNull(properties, nameof(properties));
            Guard.ValidSlug(name, nameof(name));

            this.name = name;

            this.properties = properties;

            fieldsById = fields;
            fieldsByName = fields.Values.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }

        public static Schema Create(string name, SchemaProperties newProperties)
        {
            newProperties = newProperties ?? new SchemaProperties(null, null);

            if (!name.IsSlug())
            {
                var error = new ValidationError("Name must be a valid slug", "Name");

                throw new ValidationException("Cannot create a new schema", error);
            }

            return new Schema(name, newProperties, ImmutableDictionary<long, Field>.Empty);
        }

        public Schema Update(SchemaProperties newProperties)
        {
            Guard.NotNull(newProperties, nameof(newProperties));

            return new Schema(name, newProperties, fieldsById);
        }

        public Schema AddOrUpdateField(Field field)
        {
            Guard.NotNull(field, nameof(field));

            if (fieldsById.Values.Any(f => f.Name == field.Name && f.Id != field.Id))
            {
                throw new ValidationException($"A field with name '{field.Name}' already exists.");
            }

            return new Schema(name, properties, fieldsById.SetItem(field.Id, field));
        }

        public Schema UpdateField(long fieldId, IFieldProperties newProperties)
        {
            return UpdateField(fieldId, field => field.Update(newProperties));
        }

        public Schema DisableField(long fieldId)
        {
            return UpdateField(fieldId, field => field.Disable());
        }

        public Schema EnableField(long fieldId)
        {
            return UpdateField(fieldId, field => field.Enable());
        }

        public Schema HideField(long fieldId)
        {
            return UpdateField(fieldId, field => field.Show());
        }

        public Schema ShowField(long fieldId)
        {
            return UpdateField(fieldId, field => field.Show());
        }

        public Schema RenameField(long fieldId, string newName)
        {
            return UpdateField(fieldId, field => field.Rename(newName));
        }

        public Schema DeleteField(long fieldId)
        {
            return new Schema(name, properties, fieldsById.Remove(fieldId));
        }

        public Schema UpdateField(long fieldId, Func<Field, Field> updater)
        {
            Guard.NotNull(updater, nameof(updater));

            Field field;

            if (!fieldsById.TryGetValue(fieldId, out field))
            {
                throw new DomainObjectNotFoundException(fieldId.ToString(), typeof(Field));
            }

            var newField = updater(field);

            return AddOrUpdateField(newField);
        }

        public async Task ValidateAsync(PropertiesBag data, IList<ValidationError> errors)
        {
            Guard.NotNull(data, nameof(data));
            Guard.NotNull(errors, nameof(errors));

            foreach (var kvp in data.Properties)
            {
                var fieldErrors = new List<string>();

                Field field;

                if (fieldsByName.TryGetValue(kvp.Key, out field))
                {
                    await field.ValidateAsync(kvp.Value, fieldErrors);
                }
                else
                {
                    fieldErrors.Add($"'{kvp.Key}' is not a known field");
                }

                fieldErrors.ForEach(error => errors.Add(new ValidationError(error, kvp.Key)));
            }
        }
    }
}