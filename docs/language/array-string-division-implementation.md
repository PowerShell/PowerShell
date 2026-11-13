# Array and String Division Operator Implementation

## Overview

This implementation extends PowerShell's division operator (`/`) to support array and string division operations, providing intuitive syntax for splitting collections and strings into smaller parts.

## Features

### Array Division
- **Equal Distribution**: `@(1,2,3,4,5,6) / 2` → Two groups: `[1,2,3]`, `[4,5,6]`  
- **Custom Sizes**: `@(1,2,3,4,5,6,7,8) / @(2,3,2)` → Groups: `[1,2]`, `[3,4,5]`, `[6,7]`, `[8]`
- **Uneven Distribution**: Automatically handles remainders by distributing extra elements to earlier groups

### String Division  
- **Character-based Splitting**: `'hello world' / 2` → Two parts: `'hello '`, `'world'`
- **Custom Length Splitting**: `'abcdefghij' / @(3,4,2)` → Parts: `'abc'`, `'defg'`, `'hi'`, `'j'`
- **Preserves Unicode**: Correctly handles special characters, emojis, and Unicode text

## Implementation Architecture

### Core Components

1. **ArrayOps.cs** - Array division logic
   ```csharp
   internal static object[] Divide(object[] lhs, object rhs)
   ```

2. **StringOps.cs** - String division logic  
   ```csharp
   internal static string[] Divide(string lhs, object rhs)
   ```

3. **Binders.cs** - Dynamic binding integration
   - Intercepts `/` operator for arrays and strings
   - Falls back to numeric division for other types

4. **Compiler.cs** - Cached reflection optimization
   - `ArrayOps_Divide` and `StringOps_Divide` method info caching

### Algorithm Design

#### Array Division with Integer Divisor
```
elementsPerGroup = array.Length / divisor
remainder = array.Length % divisor
// First 'remainder' groups get one extra element
```

#### Array Division with Size Array
```
foreach size in sizeArray:
    create group of specified size
    move to next position
// Remaining elements form final group if any
```

#### String Division
Uses similar logic but operates on character positions instead of array indices.

## Testing Strategy

### Test Coverage
- **Unit Tests**: Individual operator behaviors
- **Integration Tests**: Interaction with PowerShell runtime  
- **Edge Cases**: Empty inputs, single elements, error conditions
- **Performance Tests**: Large datasets, memory efficiency
- **Compatibility Tests**: Backward compatibility with existing division

### Test Files
- `ArrayDivision.Tests.ps1` - Comprehensive array division tests
- `StringDivision.Tests.ps1` - Complete string division test suite  
- `DivisionOperatorIntegration.Tests.ps1` - Integration and compatibility tests

### Running Tests
```powershell
# Run specific test file
Invoke-Pester -Path "test/powershell/Language/Operators/ArrayDivision.Tests.ps1"

# Run all division operator tests
Invoke-Pester -Path "test/powershell/Language/Operators/*Division*.Tests.ps1"

# Run integration tests
Invoke-Pester -Path "test/powershell/Language/Operators/DivisionOperatorIntegration.Tests.ps1"
```

## Usage Examples

### Basic Usage
```powershell
# Array division into equal groups
$data = @(1,2,3,4,5,6,7,8)
$groups = $data / 4  # Creates 4 groups of 2 elements each

# String division into equal parts  
$text = "Hello World Testing"
$parts = $text / 3   # Splits into 3 roughly equal parts
```

### Advanced Usage
```powershell
# Custom group sizes
$array = 1..20
$customGroups = $array / @(5,7,3,2)  # Groups of 5,7,3,2 elements + remainder

# Pipeline integration
$data | Where-Object { $_ -gt 5 } | ForEach-Object { @($_) / 2 }

# Variable assignment
$groupSize = 3
$result = @("a","b","c","d","e","f") / $groupSize
```

## Error Handling

### Validation Rules
- Divisor must be positive integer or array of positive integers
- Empty divisor arrays are rejected
- Type conversion attempted for compatible types (e.g., "2" → 2)

### Error Messages
```powershell
# Invalid divisor
@(1,2,3) / 0          # "Array division requires a positive divisor"
@(1,2,3) / @()        # "Array division requires positive integer sizes"  
@(1,2,3) / @(2,0,1)   # "Array division requires positive integer sizes"
```

## Performance Characteristics

### Time Complexity
- Array Division: O(n) where n is array length
- String Division: O(n) where n is string length  
- Memory: O(n) for result storage

### Optimizations
- Cached reflection reduces method call overhead
- Efficient array copying for large datasets
- String.Substring() for optimal string operations
- Minimal temporary object creation

## Backward Compatibility

### Preserved Behaviors
- All existing numeric division operations unchanged
- Error messages for unsupported types maintained  
- PowerShell type coercion rules respected
- Pipeline and parameter binding compatibility

### Migration Path
No breaking changes - new functionality only activates for array and string types.

## Future Enhancements

### Potential Extensions
1. **Matrix Division**: 2D array splitting capabilities
2. **Regex Division**: Pattern-based string splitting with `/` operator
3. **Stream Division**: IEnumerable support for large datasets
4. **Custom Separator**: String division with delimiter specification

### Performance Improvements
1. **Lazy Evaluation**: Defer group creation until accessed
2. **Memory Pooling**: Reuse arrays for large operations
3. **SIMD Operations**: Vectorized operations for numeric arrays

## Development Guidelines

### Code Standards
- Follow existing PowerShell codebase conventions
- Include comprehensive XML documentation
- Add appropriate error handling and validation
- Maintain consistent naming patterns

### Testing Requirements
- Minimum 95% code coverage for new functionality
- Include edge case and error condition tests
- Performance regression tests for large inputs
- Cross-platform compatibility verification

### Review Checklist
- [ ] Backward compatibility verified
- [ ] Error messages are clear and actionable  
- [ ] Performance impact assessed
- [ ] Security implications reviewed
- [ ] Documentation updated
- [ ] Tests cover all code paths