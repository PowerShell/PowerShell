#!/usr/bin/python3

import json

# Define a class with a method that returns JSON
class returnsjson:
    def method1(this):
        return json.dumps(['foo',
                            {
                                'bar': ('baz', None, 1.0, 2),
                                'buz': ('foo1', 'foo2', 'foo3')
                            },
                            'alpha',
                            1,2,3])

c = returnsjson()
print (c.method1())
                                  
