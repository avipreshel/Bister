# Bister

A binary serializer for C#, which is based on run time code generation, implemented in dotnet standard 2.0.
* Offers better performance than System.Text.Json (see benchmarks below)
* Offers better type coverage than both System.Text.Json and Newtonsoft (For example: Ability to serialize and de-serialize Generics with Enum type)
* Implemented in pure dotnet standard 2.0 and does not rely on any 3rd party library

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
# Wish to see the generated class? no problem...
```cs

Bister.Instance.DebugPath = @"c:\temp\generatedcode.cs"; // Will dump the generated code into this file
SomeClass instance = new();
byte[] blob = Bister.Instance.Serialize<SomeClass>(instance); // Dump will happen here
SomeClass instanceCopy = Bister.Instance.Deserialize<SomeClass>(blob); // No dump here, as class was already generated in previous call to Serialize<SomeClass>
```

# How does it work
* It uses run time reflection to discover the incoming type, and then performs following steps
  1. Discover all the Public property fields that have a public get & set accessors
  2. For each property, it generates code text (StringBuilder) that can serializes it
  3. StringBuilder output is sent to Roslyn (dotnet compiler)
  4. An assembly is created during run time, and it contains the newly defined serializer type
  5. An instance is create from that type
  6. The instance is cached in the Singelton of Bister, and then used to perform the actual serialization
     
* It serialize only Public property fields that have a public get & set accessors
* Bister also identifies Generic types, and is able to treat them accordingly
* Whenever the serialzier encounters a new type, it generates a run time serializer code to efficently serialize it to/from byte array. The generated class is then cached for further usage. Note that this behavior means that the first usage per type will incur some single run-time cost, as it takes time to create the class code and compile it, in run time.
The generated code is fully debug-able and easy to understand.

# Unsupported types
* System.Half (because it's unsupported by Dotnet standard 2.0)
 
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
