﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

namespace Kontore.Reflection {
	public abstract class ReflectionQuery<T> {
		public List<T> Result { get; }
		protected List<T> Data { get; set; }
		private List<Func<T, bool>> Queries { get; }

		public ReflectionQuery() {
			Queries = new List<Func<T, bool>>();
		}

		public ReflectionQuery(IEnumerable<Func<T, bool>> queries) {
			Queries = queries.ToList();
		}

		public ReflectionQuery(params Func<T, bool>[] queries) {
			Queries = Queries.ToList();
		}

		protected virtual ReflectionQuery<T> Run(IEnumerable<T> data, bool allQueries = true) {
			Data.Clear();
			Data.AddRange(data);

			if (Queries.Count == 0) {
				Result.AddRange(Data);
			} else {
				Result.AddRange(Data.Where(value =>
					allQueries
						? Queries.All(query => query(value))
						: Queries.Any(query => query(value))
				));
			}

			return this;
		}

		public virtual Func<T, bool> Get(int index) => Queries[index];
		public virtual ReflectionQuery<T> Set(int index, Func<T, bool> query) {
			Queries[index] = query;

			return this;
		}

		public virtual ReflectionQuery<T> Add(Func<T, bool> query) {
			Queries.Add(query);

			return this;
		}

		public virtual ReflectionQuery<T> RemoveAt(int index) {
			Queries.RemoveAt(index);

			return this;
		}

		public virtual ReflectionQuery<T> Remove(Func<T, bool> query) {
			Queries.Remove(query);

			return this;
		}
	}
	
	public class EnumQuery<TEnum> : ReflectionQuery<TEnum> where TEnum : Enum {
		public EnumQuery() : base() { }
		public EnumQuery(IEnumerable<Func<TEnum, bool>> queries) : base(queries) { }
		public EnumQuery(params Func<TEnum, bool>[] queries) : base(queries) { }

		public EnumQuery<TEnum> Run(bool allQueries = true) {
			base.Run(Enum.GetValues(typeof(TEnum)).OfType<TEnum>(), allQueries);
			return this;
		}

		public bool Any() => Result.Count > 0;
		public bool All() => Result.Count == Enum.GetValues(typeof(TEnum)).Length;
		public int Sum() {
			var values = Enum.GetValues(typeof(TEnum));
			int total = 0;

			foreach (int value in values) {
				total += value;
			}

			return total;
		}
	}

	public class PropertyInfoQuery : ReflectionQuery<PropertyInfo> {
		public PropertyInfoQuery() : base() { }
		public PropertyInfoQuery(IEnumerable<Func<PropertyInfo, bool>> queries) : base(queries) { }
		public PropertyInfoQuery(params Func<PropertyInfo, bool>[] queries) : base(queries) { }

		public ReflectionQuery<PropertyInfo> Run<T>(T source, bool allQueries = true) {
			base.Run(typeof(T).GetProperties(), allQueries);
			return this;
		}

		public PropertyInfo Of(Type sourceType, string name) {
			return Result.Find(info => info.DeclaringType == sourceType && info.Name == name);
		}

		public PropertyInfo OfPath(Type sourceType, string path) {
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("The path must not be null or whitespace.", nameof(path));

			var fragments = path.Split('.');
			var current = Of(sourceType, fragments[0]);

			for (int i = 1; i < fragments.Length; i++) {
				current = Of(current.PropertyType, fragments[i]);
			}

			return current;
		}
	}

	public class TypeQuery : ReflectionQuery<Type> {
		public TypeQuery() : base() { }
		public TypeQuery(IEnumerable<Func<Type, bool>> queries) : base(queries) { }
		public TypeQuery(params Func<Type, bool>[] queries) : base(queries) { }

		public TypeQuery Run(IEnumerable<Type> types, bool allQueries = true) {
			base.Run(types, allQueries);
			return this;
		}

		public PropertyInfo GetProperty(string typeName, string path) {
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("The path must not be null or whitespace.", nameof(path));

			var fragments = path.Split('.');
			var current = Result.Find(type => type.FullName == typeName).GetProperty(fragments[0]);

			for (int i = 1; i < fragments.Length; i++) {
				current = current.PropertyType.GetProperty(fragments[i]);
			}

			return current;
			//return Result.Find(type => type.FullName == typeName).GetProperty(path);
		}

		public object GetPropertyValue(string typeName, string name, object source) {
			return GetProperty(typeName, name).GetValue(source);
		}
	}

	public class a {
		public enum Cat {
			Orange = 1 << 0,
			White = 1 << 1,
			Black = 1 << 2,
			Brown = 1 << 4,
			LongFur = 1 << 5
		}

		public void b() {
			var isOrange = new Func<Cat, bool>(cat => cat.HasFlag(Cat.Orange));
			var shortFur = new Func<Cat, bool>(cat => !cat.HasFlag(Cat.LongFur));
			var query = new EnumQuery<Cat>(isOrange);
			query.Add(shortFur);
			query.Run();
			query.Sum();
			var typeQuery = new TypeQuery().Run(new[] { typeof(string) });
			typeQuery.GetProperty("string", "Length");
		}
	}
}
