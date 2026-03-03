import { Component } from '@angular/core';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BookService, BookUpsert } from '../../services/book.service';

@Component({
  selector: 'app-book-detail',
  standalone: true,
  imports: [FormsModule, NgIf, RouterLink],
  templateUrl: './book-detail.component.html',
})
export class BookDetailComponent {
  id: string | null = null;

  title = '';
  author = '';
  year = new Date().getFullYear();
  status = 'Planned';

  loading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private books: BookService
  ) {
    this.id = this.route.snapshot.paramMap.get('id');

    if (this.id) {
      this.loading = true;
      this.books.getBook(this.id).subscribe({
        next: (b) => {
          this.title = b.title;
          this.author = b.author;
          this.year = b.year;
          this.status = b.status;
          this.loading = false;
        },
        error: () => {
          this.error = 'Book not found or failed to load.';
          this.loading = false;
        }
      });
    }
  }

  private payload(): BookUpsert {
    return {
      title: this.title.trim(),
      author: this.author.trim(),
      year: this.year,
      status: this.status
    };
  }

  save(): void {
    this.error = null;

    if (!this.title.trim() || !this.author.trim()) {
      this.error = 'Title and Author are required.';
      return;
    }

    const req = this.payload();

    if (!this.id) {
      // CREATE
      this.books.createBook(req).subscribe({
        next: () => this.router.navigateByUrl('/books'),
        error: () => (this.error = 'Failed to create book.')
      });
    } else {
      // UPDATE
      this.books.updateBook(this.id, req).subscribe({
        next: () => this.router.navigateByUrl('/books'),
        error: () => (this.error = 'Failed to update book.')
      });
    }
  }

  delete(): void {
    if (!this.id) return;
    if (!confirm('Delete this book?')) return;

    this.books.deleteBook(this.id).subscribe({
      next: () => this.router.navigateByUrl('/books'),
      error: () => (this.error = 'Failed to delete book.')
    });
  }
}
