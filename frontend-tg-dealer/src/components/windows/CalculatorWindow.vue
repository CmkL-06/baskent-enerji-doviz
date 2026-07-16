<template>
  <div class="calculator-window">
    <div class="calculator-display">
      <div class="display-history">{{ history }}</div>
      <div class="display-current">{{ display }}</div>
    </div>

    <div class="calculator-buttons">
      <!-- Row 1 -->
      <button @click="clear" class="btn-function btn-clear">C</button>
      <button @click="clearEntry" class="btn-function">CE</button>
      <button @click="percentage" class="btn-function">%</button>
      <button @click="setOperator('/')" class="btn-operator">÷</button>

      <!-- Row 2 -->
      <button @click="inputNumber('7')" class="btn-number">7</button>
      <button @click="inputNumber('8')" class="btn-number">8</button>
      <button @click="inputNumber('9')" class="btn-number">9</button>
      <button @click="setOperator('*')" class="btn-operator">×</button>

      <!-- Row 3 -->
      <button @click="inputNumber('4')" class="btn-number">4</button>
      <button @click="inputNumber('5')" class="btn-number">5</button>
      <button @click="inputNumber('6')" class="btn-number">6</button>
      <button @click="setOperator('-')" class="btn-operator">−</button>

      <!-- Row 4 -->
      <button @click="inputNumber('1')" class="btn-number">1</button>
      <button @click="inputNumber('2')" class="btn-number">2</button>
      <button @click="inputNumber('3')" class="btn-number">3</button>
      <button @click="setOperator('+')" class="btn-operator">+</button>

      <!-- Row 5 -->
      <button @click="toggleSign" class="btn-function">±</button>
      <button @click="inputNumber('0')" class="btn-number">0</button>
      <button @click="inputDecimal" class="btn-function">.</button>
      <button @click="calculate" class="btn-equals">=</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onUnmounted } from 'vue';

// Calculator state
const display = ref('0');
const previousValue = ref<number | null>(null);
const currentOperator = ref<string | null>(null);
const waitingForOperand = ref(false);
const history = ref('');

// Input handlers
const inputNumber = (num: string) => {
  if (waitingForOperand.value) {
    display.value = num;
    waitingForOperand.value = false;
  } else {
    display.value = display.value === '0' ? num : display.value + num;
  }
};

const inputDecimal = () => {
  if (waitingForOperand.value) {
    display.value = '0.';
    waitingForOperand.value = false;
  } else if (display.value.indexOf('.') === -1) {
    display.value += '.';
  }
};

const clear = () => {
  display.value = '0';
  previousValue.value = null;
  currentOperator.value = null;
  waitingForOperand.value = false;
  history.value = '';
};

const clearEntry = () => {
  display.value = '0';
};

const toggleSign = () => {
  const num = parseFloat(display.value);
  display.value = String(-num);
};

const percentage = () => {
  const num = parseFloat(display.value);
  display.value = String(num / 100);
};

const setOperator = (operator: string) => {
  const inputValue = parseFloat(display.value);

  if (previousValue.value === null) {
    previousValue.value = inputValue;
  } else if (currentOperator.value) {
    const currentValue = previousValue.value || 0;
    const newValue = performOperation(currentValue, inputValue, currentOperator.value);
    
    display.value = String(newValue);
    previousValue.value = newValue;
  }

  waitingForOperand.value = true;
  currentOperator.value = operator;
  
  // Update history
  history.value = `${previousValue.value} ${getOperatorSymbol(operator)}`;
};

const calculate = () => {
  const inputValue = parseFloat(display.value);

  if (previousValue.value !== null && currentOperator.value) {
    const newValue = performOperation(previousValue.value, inputValue, currentOperator.value);
    
    // Update history to show full calculation
    history.value = `${previousValue.value} ${getOperatorSymbol(currentOperator.value)} ${inputValue} =`;
    
    display.value = String(newValue);
    previousValue.value = null;
    currentOperator.value = null;
    waitingForOperand.value = true;
  }
};

const performOperation = (firstValue: number, secondValue: number, operator: string): number => {
  switch (operator) {
    case '+':
      return firstValue + secondValue;
    case '-':
      return firstValue - secondValue;
    case '*':
      return firstValue * secondValue;
    case '/':
      return secondValue !== 0 ? firstValue / secondValue : 0;
    default:
      return secondValue;
  }
};

const getOperatorSymbol = (operator: string): string => {
  switch (operator) {
    case '*': return '×';
    case '/': return '÷';
    case '-': return '−';
    default: return operator;
  }
};

// Keyboard support
const handleKeyboard = (event: KeyboardEvent) => {
  const key = event.key;
  
  if (key >= '0' && key <= '9') {
    inputNumber(key);
  } else if (key === '.') {
    inputDecimal();
  } else if (key === '+' || key === '-' || key === '*' || key === '/') {
    setOperator(key);
  } else if (key === 'Enter' || key === '=') {
    calculate();
  } else if (key === 'Escape' || key === 'c' || key === 'C') {
    clear();
  } else if (key === '%') {
    percentage();
  }
};

// Add keyboard event listener
if (typeof window !== 'undefined') {
  window.addEventListener('keydown', handleKeyboard);
}

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyboard);
})
</script>

<style scoped>
.calculator-window {
  padding: 16px;
  background: #1e1e1e;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.calculator-display {
  background: #2d2d2d;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 16px;
  text-align: right;
}

.display-history {
  font-size: 14px;
  color: #999;
  min-height: 20px;
  margin-bottom: 4px;
}

.display-current {
  font-size: 32px;
  font-weight: 300;
  color: white;
  word-wrap: break-word;
  word-break: break-all;
}

.calculator-buttons {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px;
  flex: 1;
}

.calculator-buttons button {
  border: none;
  border-radius: 8px;
  font-size: 20px;
  font-weight: 400;
  cursor: pointer;
  transition: background-color 0.2s, color 0.2s;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-number {
  background: #3a3a3a;
}

.btn-number:hover {
  background: #4a4a4a;
}

.btn-number:active {
  transform: scale(0.95);
}

.btn-function {
  background: #2d2d2d;
}

.btn-function:hover {
  background: #3d3d3d;
}

.btn-operator {
  background: #ff9500;
}

.btn-operator:hover {
  background: #ffb143;
}

.btn-operator:active {
  background: #cc7700;
}

.btn-equals {
  background: #4CAF50;
}

.btn-equals:hover {
  background: #5cbf60;
}

.btn-equals:active {
  background: #3d8b40;
}

.btn-clear {
  background: #f44336;
}

.btn-clear:hover {
  background: #f66;
}
</style>