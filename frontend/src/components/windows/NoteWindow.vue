<template>
  <div class="note-window">
    <div class="note-header">
      <input
        v-model="noteTitle"
        @change="updateNote"
        class="note-title"
        placeholder="Not Başlığı"
      />
      <div class="note-actions">
        <button @click="saveNote" class="btn-icon" title="Kaydet">
          💾
        </button>
        <button @click="deleteNote" class="btn-icon btn-danger" title="Sil">
          🗑️
        </button>
      </div>
    </div>

    <textarea
      v-model="noteContent"
      @input="updateNote"
      class="note-content"
      placeholder="Notunuzu yazmaya başlayın..."
    ></textarea>

    <div class="note-footer">
      <span class="note-date">
        Son güncelleme: {{ formatDate(note?.updatedAt || new Date()) }}
      </span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { useDesktopStore } from '../../stores/desktop';
import type { Note } from '../../types';

const props = defineProps<{
  data?: Note;
}>();

const emit = defineEmits<{
  close: [];
}>();

const desktopStore = useDesktopStore();

// Note state
const note = ref<Note | null>(props.data || null);
const noteTitle = ref('');
const noteContent = ref('');

// Initialize note data
onMounted(() => {
  if (note.value) {
    noteTitle.value = note.value.title;
    noteContent.value = note.value.content;
  }
});

// Update note
const updateNote = () => {
  if (note.value) {
    desktopStore.updateNote(note.value.id, {
      title: noteTitle.value,
      content: noteContent.value,
    });
  }
};

// Save note (visual feedback)
const saveNote = () => {
  updateNote();
  // Could add a visual confirmation here
};

// Delete note
const deleteNote = () => {
  if (note.value && confirm('Bu notu silmek istediğinizden emin misiniz?')) {
    desktopStore.deleteNote(note.value.id);
    emit('close');
  }
};

// Format date
const formatDate = (date: Date | string) => {
  const d = new Date(date);
  return d.toLocaleString('tr-TR', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

// Auto-save on content change
let saveTimeout: ReturnType<typeof setTimeout>;
watch([noteTitle, noteContent], () => {
  clearTimeout(saveTimeout);
  saveTimeout = setTimeout(() => {
    updateNote();
  }, 1000); // Auto-save after 1 second of inactivity
});
</script>

<style scoped>
.note-window {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #fffef0;
}

.note-header {
  display: flex;
  align-items: center;
  padding: 16px;
  border-bottom: 1px solid #e0e0e0;
  background: white;
}

.note-title {
  flex: 1;
  font-size: 18px;
  font-weight: 500;
  border: none;
  outline: none;
  background: transparent;
  color: #333;
  padding: 4px 8px;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.note-title:hover,
.note-title:focus {
  background: #f5f5f5;
}

.note-actions {
  display: flex;
  gap: 8px;
}

.btn-icon {
  width: 32px;
  height: 32px;
  border: none;
  background: transparent;
  border-radius: 4px;
  cursor: pointer;
  font-size: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.btn-icon:hover {
  background: #f0f0f0;
}

.btn-danger:hover {
  background: #ffebee;
}

.note-content {
  flex: 1;
  padding: 16px;
  border: none;
  outline: none;
  resize: none;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  font-size: 14px;
  line-height: 1.6;
  color: #333;
  background: transparent;
}

.note-content::placeholder {
  color: #999;
}

.note-footer {
  padding: 8px 16px;
  border-top: 1px solid #e0e0e0;
  background: white;
}

.note-date {
  font-size: 12px;
  color: #666;
}
</style>