# Bister

A binary serializer for C#, which is based on run time code generation, implemented in dotnet standard 2.0.

Whenever the serialzier encounters a new type, it generates a run time serializer code to efficently serialize it to/from byte array.

Note that this behavior means that the first usage per type will incur some single run-time cost, as it takes time to create the class code and compile it, in run time.
The generated code is fully debug-able and easy to understand.

# Advantage compared Json
* Better performance (See benchmarks below)
* Usage of binary means that the serialized class consumes less bytes
* Ability to handle Enum type, including List<Enum>

# Disadvantage compared to Json
The generated binary is not backward compatible, which means that using it in persistency scenarios is risky:
1) Serialzie a class
2) Save the byte array to File
3) Modify the class structure, in any way
4) Read the byte array from the file == This will fail!

# Usage
```cs
SomeClass instance = new();
// Serialize
// Bister.Instance returns a Singelton instance of IBister, which means that Bister can easily fit with any dependency injection framework
// The instance is Thread safe, lockless and can be shared by multiple threads, as the generated class is state-less.
byte[] blob = Bister.Instance.Serialize<SomeClass>(instance);

// De-Serialize
SomeClass instanceCopy = Bister.Instance.Deserialize<SomeClass>(blob);
```

# Benchmarks
Done on a class that contains Dictionary<string,float> and List<string> fields.

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.4170/22H2/2022Update)
Intel Core i7-6800K CPU 3.40GHz (Skylake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.303
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2 [AttachedDebugger]
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


| Method            | Mean      | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|------------------ |----------:|---------:|---------:|-------:|-------:|----------:|
| JsonSerialize     | 381.68 ns | 7.473 ns | 8.897 ns | 0.0138 |      - |     112 B |
| BisterSerialize   |  95.85 ns | 1.975 ns | 3.407 ns | 0.0254 |      - |     200 B |
| JsonDeserialize   | 465.82 ns | 1.752 ns | 1.639 ns | 0.0091 |      - |      72 B |
| BisterDeserialize | 153.64 ns | 2.520 ns | 2.234 ns | 0.0937 | 0.0002 |     736 B |
