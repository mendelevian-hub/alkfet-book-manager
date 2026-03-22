import { Routes } from '@angular/router';
import { BookListComponent } from './pages/book-list/book-list.component';
import { BookDetailComponent } from './pages/book-detail/book-detail.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'books' },

  // View #1: list + pagination (később)
  { path: 'books', component: BookListComponent },

  // View #2: create/edit (ugyanaz a képernyő)
  { path: 'books/new', component: BookDetailComponent },
  { path: 'books/:id', component: BookDetailComponent },

  // fallback
  { path: '**', redirectTo: 'books' }
];
