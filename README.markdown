Benchmarque - Benchmarking for .NET Applications
================================================

## Building from Source

 1. Clone the source down to your machine.
   `git clone git://github.com/phatboyg/Benchmarque.git`
 1. Ensure Ruby is installed. [RubyInstaller for Windows](http://rubyinstaller.org/)
 1. Ensure `git` is on your path. `git.exe` and `git.cmd` work equally well.
 1. **Ensure gems are installed**, run:

```
gem install albacore
gem install semver2
```

4. Run `rake`

## Note on Git Submodules

You need to ensure that the submodules are up-to-date. To do this, once you've cloned
the project (using close --recursive), you can use the following command to update
to the latest version of the submodules.

```
git submodule foreach git fetch origin
git submodule foreach git rebase origin/master
```

## Contributing

 1. `git config --global core.autoclrf false`
 1. Hack!
 1. Make a pull request.

# REQUIREMENTS
* .Net 4.0
