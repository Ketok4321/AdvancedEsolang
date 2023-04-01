# Advanced Esolang
Advanced is an object-oriented esolang in which there are no primitives.
That means that the only way to store data in Advanced is either by the type of the Object, or by its instance.
For example, ClassNumber (which is one of implementations of numbers for Advanced) stores data using the type of the object, which means that there's a class for every supported number.

## Running Advanced Cli
To both run, and compile Advanced, .NET 6.0 is required.

To compile the project, run:
```
dotnet build -c Release
```
The executable will then be located in `AdvancedEsolang.Cli/bin/Release/net6.0/AdvancedEsolang.Cli`.

## Running Advanced programs
There are a few example programs located in the `samples` directory of the repo, in order to run them you will need their dependencies, which are located in `std` (which is found in the repo) and `generated` (which you will need to generate manually).

### Generating dependencies
To generate files, use the `generate` subcommand.
```
{path to advanced cli} generate {name of the generator} -c {count of the things to generate} -o {output file}
```

To run all of the avaiable generators with sane-ish defaults:
```
{path to advanced cli} generate class_number -c 10000 -o generated/class_number.adv
{path to advanced cli} generate instance_number -c 10000 -o generated/instance_number.adv
{path to advanced cli} generate binary_number -c 8 -o generated/binary_number.adv
{path to advanced cli} generate array -c 16 -o generated/array.adv
{path to advanced cli} generate builtin -o generated/builtin.adv
```
All of those generators generate their respective libraries, with the exception of the `builtin` generator which dumps the contents of the builtin library (mostly for debug purposes, no program will depend on that).

### Actually running Advanced programs

To run an Advanced program:
```
{path to advanced cli} run {path to the file}
```
