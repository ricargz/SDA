import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CreateTaskRequest, TaskDto } from '../../models/task.model';
import { TaskService } from '../../services/task.service';

@Component({
  selector: 'app-task-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './task-dialog.component.html',
  styleUrls: ['./task-dialog.component.scss']
})
export class TaskDialogComponent {
  private readonly formBuilder = inject(FormBuilder);

  readonly form = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(80), this.noWhitespaceValidator]],
    description: ['', [Validators.maxLength(240)]]
  });

  isSaving = false;

  constructor(
    private readonly taskService: TaskService,
    private readonly dialogRef: MatDialogRef<TaskDialogComponent, TaskDto>,
    private readonly snackBar: MatSnackBar
  ) {}

  get title() {
    return this.form.controls.title;
  }

  get description() {
    return this.form.controls.description;
  }

  save(): void {
    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    const request: CreateTaskRequest = {
      title: this.title.value?.trim() ?? '',
      description: this.description.value?.trim() ?? ''
    };

    this.isSaving = true;
    this.taskService.createTask(request).subscribe({
      next: (task) => {
        this.snackBar.open('Tarea creada correctamente.', 'Cerrar', { duration: 3000 });
        this.dialogRef.close(task);
      },
      error: () => {
        this.isSaving = false;
        this.snackBar.open('No se pudo crear la tarea. Revisa tu conexion e intenta de nuevo.', 'Cerrar', {
          duration: 5000
        });
      }
    });
  }

  cancel(): void {
    if (!this.isSaving) {
      this.dialogRef.close();
    }
  }

  private noWhitespaceValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (typeof value === 'string' && value.trim().length === 0 && value.length > 0) {
      return { whitespace: true };
    }

    return null;
  }
}
