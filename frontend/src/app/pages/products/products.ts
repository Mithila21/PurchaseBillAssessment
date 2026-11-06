
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Product {
  id: number;
  name: string;
  category: string;
  price: number;
  quantity: number;
}

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './products.html',
  styleUrl: './products.css'
})
export class ProductsComponent implements OnInit {

  private http = inject(HttpClient);

  products = signal<Product[]>([]);
  message = signal('');
  isSaving = signal(false);

  productForm = new FormGroup({
    productName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    category: new FormControl('Fruits', {
    nonNullable: true,
    validators: [Validators.required]
    }),
    price: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)]
    }),
    quantity: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    })
  });

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.http.get<Product[]>('/api/products').subscribe({
      next: (data) => {
        this.products.set(data);
      },
      error: () => {
        this.message.set('Failed to load products');
      }
    });
  }

  saveProduct() {
    if (this.productForm.invalid || this.isSaving()) {
      return;
    }

    const product = this.productForm.getRawValue();

    if (!product.productName.trim()) {
      this.message.set('Product name is required');
      return;
    }

    this.isSaving.set(true);
    this.message.set('');

    this.http.post('/api/products', product).subscribe({
      next: () => {
        this.message.set('Product saved successfully!');
        this.productForm.reset({
          productName: '',
          category: 'Fruits',
          price: 0,
          quantity: 1
        });
        this.isSaving.set(false);
        this.loadProducts();
      },
      error: () => {
        this.message.set('Failed to save product');
        this.isSaving.set(false);
      }
    });
  }
}
