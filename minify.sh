#!/bin/bash

# not really minify, just remove some chars
sed -i 's/^\s\+//g' "$@"
