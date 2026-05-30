import { Component } from '@angular/core';
import { KanbanComponent } from './components/kanban/kanban.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [KanbanComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'TaskManagement-UI';
}
