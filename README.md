# Advanced Esolang (WIP)
An object-oriented esolang.<br>
Its name comes from the fact that it has no primitives, and basic things like numbers or arrays must be implemented using classes.

## Running Advanced programs
There are a few example programs located in the `samples` directory of the repo, in order to run them you will need their dependencies, which are located in `std` (which is found in the repo) and `generated` (which you will need to generate manually).

### Generating dependencies
To generate files, use the `generate` subcommand.
```
{path to advanced cli} generate {name of the generator} -c {count of the things to generate} -o {output file}
```

You can generate all of them with recommended settings using this command:
```
{path to advanced cli} generate class_number -n 10000 -o generated/class_number.adv
{path to advanced cli} generate instance_number -n 10000 -o generated/instance_number.adv
{path to advanced cli} generate binary_number -n 8 -o generated/binary_number.adv
{path to advanced cli} generate array -n 16 -o generated/array.adv
{path to advanced cli} generate builtin -n 0 -o generated/builtin.adv
```
All of those generators generate their respective libraries, with the exception of the `builtin` generator which dumps the contents of the builtin library (mostly for debug purposes, no program will depend on that).

### Actually running Advanced programs

To run an Advanced program:
```
{path to advanced cli} run {path to the file}
```
