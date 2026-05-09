<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type TodoItem = {
  id: number
  title: string
  isDone: boolean
}

const appTitle = import.meta.env.VITE_APP_TITLE || 'Todo List'
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5200'
const apiUsername = import.meta.env.VITE_API_USERNAME || 'admin'
const apiPassword = import.meta.env.VITE_API_PASSWORD || 'admin'
const logLevel = import.meta.env.VITE_LOG_LEVEL || 'info'

const levelOrder = { debug: 10, info: 20, warn: 30, error: 40 } as const
type LevelName = keyof typeof levelOrder

function log(level: LevelName, message: string, payload?: unknown): void {
  const current = levelOrder[(logLevel.toLowerCase() as LevelName) ?? 'info'] ?? levelOrder.info
  if (levelOrder[level] < current) {
    return
  }

  const entry = {
    ts: new Date().toISOString(),
    source: 'todoweb',
    level,
    message,
    payload,
  }

  if (level === 'error') {
    console.error(entry)
    return
  }

  if (level === 'warn') {
    console.warn(entry)
    return
  }

  console.info(entry)
}

function authHeader(): HeadersInit {
  const encoded = btoa(`${apiUsername}:${apiPassword}`)
  return { Authorization: `Basic ${encoded}` }
}

const todos = ref<TodoItem[]>([])
const newTodoTitle = ref('')
const isLoading = ref(false)
const isSubmitting = ref(false)
const errorMessage = ref('')

const pendingCount = computed(() => todos.value.filter((todo) => !todo.isDone).length)

async function loadTodos(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(`${apiBaseUrl}/api/todos`, {
      headers: authHeader(),
    })
    if (!response.ok) {
      throw new Error(`Failed to load todos: ${response.status}`)
    }

    const data = (await response.json()) as TodoItem[]
    todos.value = data
    log('info', 'Todos loaded', { count: data.length })
  } catch (error) {
    errorMessage.value = 'Nao foi possivel carregar a lista de tarefas.'
    log('error', 'Error loading todos', error)
  } finally {
    isLoading.value = false
  }
}

async function addTodo(): Promise<void> {
  const title = newTodoTitle.value.trim()
  if (!title) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(`${apiBaseUrl}/api/todos`, {
      method: 'POST',
      headers: { ...authHeader(), 'Content-Type': 'application/json' },
      body: JSON.stringify({ title }),
    })

    if (!response.ok) {
      throw new Error(`Failed to create todo: ${response.status}`)
    }

    const created = (await response.json()) as TodoItem
    todos.value.push(created)
    newTodoTitle.value = ''
    log('info', 'Todo created', created)
  } catch (error) {
    errorMessage.value = 'Nao foi possivel criar a tarefa.'
    log('error', 'Error creating todo', error)
  } finally {
    isSubmitting.value = false
  }
}

async function toggleTodo(todo: TodoItem): Promise<void> {
  const updated = { title: todo.title, isDone: !todo.isDone }

  try {
    const response = await fetch(`${apiBaseUrl}/api/todos/${todo.id}`, {
      method: 'PUT',
      headers: { ...authHeader(), 'Content-Type': 'application/json' },
      body: JSON.stringify(updated),
    })

    if (!response.ok) {
      throw new Error(`Failed to update todo: ${response.status}`)
    }

    const saved = (await response.json()) as TodoItem
    const index = todos.value.findIndex((item) => item.id === todo.id)
    if (index >= 0) {
      todos.value[index] = saved
    }

    log('debug', 'Todo updated', saved)
  } catch (error) {
    errorMessage.value = 'Nao foi possivel atualizar a tarefa.'
    log('error', 'Error updating todo', error)
  }
}

async function deleteTodo(id: number): Promise<void> {
  try {
    const response = await fetch(`${apiBaseUrl}/api/todos/${id}`, {
      method: 'DELETE',
      headers: authHeader(),
    })

    if (!response.ok && response.status !== 404) {
      throw new Error(`Failed to delete todo: ${response.status}`)
    }

    todos.value = todos.value.filter((todo) => todo.id !== id)
    log('info', 'Todo deleted', { id })
  } catch (error) {
    errorMessage.value = 'Nao foi possivel remover a tarefa.'
    log('error', 'Error deleting todo', error)
  }
}

onMounted(() => {
  log('info', 'Todo web started', { apiBaseUrl, logLevel, apiUsername })
  void loadTodos()
})
</script>

<template>
  <main class="layout">
    <section class="card">
      <header class="header">
        <h1>{{ appTitle }}</h1>
        <p>{{ pendingCount }} pendente(s)</p>
      </header>

      <form class="new-todo" @submit.prevent="addTodo">
        <input v-model="newTodoTitle" type="text" placeholder="Nova tarefa" :disabled="isSubmitting" />
        <button type="submit" :disabled="isSubmitting || !newTodoTitle.trim()">Adicionar</button>
      </form>

      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
      <p v-if="isLoading" class="hint">Carregando...</p>

      <ul v-else class="todo-list">
        <li v-for="todo in todos" :key="todo.id" class="todo-item">
          <label>
            <input type="checkbox" :checked="todo.isDone" @change="toggleTodo(todo)" />
            <span :class="{ done: todo.isDone }">{{ todo.title }}</span>
          </label>
          <button type="button" class="delete" @click="deleteTodo(todo.id)">Remover</button>
        </li>
      </ul>
    </section>
  </main>
</template>
