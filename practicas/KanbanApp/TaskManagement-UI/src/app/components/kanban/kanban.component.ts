import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DragDropModule, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TaskDto } from '../../models/task.model';
import { TaskService } from '../../services/task.service';
import { TaskDialogComponent } from '../task-dialog/task-dialog.component';

@Component({
  selector: 'app-kanban',
  standalone: true,
  imports: [
    CommonModule,
    DragDropModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './kanban.component.html',
  styleUrls: ['./kanban.component.scss']
})
export class KanbanComponent implements OnInit {
  todo: TaskDto[] = [];
  done: TaskDto[] = [];

  constructor(
    private taskService: TaskService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks() {
    this.taskService.getTasks().subscribe({
      next: (tasks) => {
        this.todo = tasks.filter(t => !t.isCompleted);
        this.done = tasks.filter(t => t.isCompleted);
      },
      error: (err) => {
        console.error('Error al cargar tareas', err);
        this.snackBar.open('No se pudieron cargar las tareas.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  openCreateTaskDialog(): void {
    const dialogRef = this.dialog.open(TaskDialogComponent, {
      width: '520px',
      maxWidth: 'calc(100vw - 32px)',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe((task) => {
      if (task) {
        this.todo = [task, ...this.todo];
      }
    });
  }

  drop(event: CdkDragDrop<TaskDto[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    const task = event.previousContainer.data[event.previousIndex];
    const movedToDone = event.container.data === this.done;

    if (!movedToDone || task.isCompleted) {
      return;
    }

    this.taskService.completeTask(task.id).subscribe({
      next: () => {
        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          event.previousIndex,
          event.currentIndex
        );
        event.container.data[event.currentIndex] = {
          ...task,
          isCompleted: true
        };
      },
      error: (err) => {
        console.error('No se pudo actualizar la tarea', err);
        this.snackBar.open('No se pudo completar la tarea. Intenta de nuevo.', 'Cerrar', { duration: 5000 });
      }
    });
  }
}
