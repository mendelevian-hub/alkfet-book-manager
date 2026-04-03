import { Component } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Book, BookService, PagedResult } from '../../services/book.service';

@Component({
  selector: 'app-book-list',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink],
  templateUrl: './book-list.component.html',
})
export class BookListComponent {
  items: Book[] = [];
  page = 1;
  pageSize = 10;
  total = 0;
  loading = false;
  error: string | null = null;

  constructor(private books: BookService) {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.books.getBooks(this.page, this.pageSize).subscribe({
      next: (res: PagedResult<Book>) => {
        this.items = res.items;
        this.total = res.total;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load books.';
        this.loading = false;
      }
    });
  }

  deleteBook(book: Book): void {
    if (!confirm(`Delete "${book.title}"?`)) return;

    this.error = null;

    this.books.deleteBook(book.id).subscribe({
      next: () => {
        // Ha az utolsó elemet törölted az oldalon, és nem az első oldalon vagy,
        // akkor visszalép egy oldalt.
        if (this.items.length === 1 && this.page > 1) {
          this.page--;
        }

        this.load();
      },
      error: () => {
        this.error = 'Failed to delete book.';
      }
    });
  }

  prev(): void {
    if (this.page <= 1) return;
    this.page--;
    this.load();
  }

  next(): void {
    if (this.page * this.pageSize >= this.total) return;
    this.page++;
    this.load();
  }
}
