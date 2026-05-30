export interface TaskDto {
  id: string;
  title: string;
  description: string;
  isCompleted: boolean;
  createdAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
}
