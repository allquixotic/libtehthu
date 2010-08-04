#!/bin/bash
rm -f libtehthu-bin-latest.zip
tar xvzf LibTehthu-windows.tar.gz
rm -f LibTehthu-windows.tar.gz
rm -f LibTehthu-windows/*.mdb
zip libtehthu-bin-latest.zip LibTehthu-windows/*
rm -rf LibTehthu-windows
