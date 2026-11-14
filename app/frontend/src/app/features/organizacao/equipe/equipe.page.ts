import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { mensagemDeErro } from '../../../core/erros/erro-api';
import { ContaDaEquipe, EquipeService } from './equipe.service';

@Component({
  selector: 'app-equipe',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-4xl px-6 py-10">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold">Equipe</h1>
          <p class="mt-1 text-sm text-slate-600">
            Gerencie as contas de Operador da sua organização.
          </p>
        </div>
        <a
          routerLink="/organizacao/equipe/nova"
          class="rounded-md bg-emerald-700 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-800"
        >
          Novo operador
        </a>
      </div>

      @if (mensagem() !== null) {
        <p class="mt-4 rounded-md bg-emerald-50 p-3 text-sm text-emerald-900" role="status">
          {{ mensagem() }}
        </p>
      }

      @if (erroDeAcao() !== null) {
        <p class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeAcao() }}
        </p>
      }

      @if (carregando()) {
        <p class="mt-6 text-slate-600" role="status">Carregando equipe…</p>
      } @else if (erroDeCarga() !== null) {
        <p class="mt-6 rounded-md bg-red-50 p-3 text-sm text-red-800" role="alert">
          {{ erroDeCarga() }}
          <button type="button" class="ml-1 font-medium underline" (click)="carregar()">
            Tentar novamente
          </button>
        </p>
      } @else if (operadores().length === 0) {
        <p class="mt-6 rounded-md bg-white p-4 text-slate-600">
          Nenhum operador cadastrado ainda. Use “Novo operador” para criar a primeira conta.
        </p>
      } @else {
        <div class="mt-6 overflow-x-auto rounded-md bg-white shadow-sm">
          <table class="min-w-full divide-y divide-slate-200">
            <caption class="sr-only">
              Operadores da organização
            </caption>
            <thead class="bg-slate-50">
              <tr>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Nome
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Usuário
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  E-mail
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  Situação
                </th>
                <th
                  scope="col"
                  class="px-4 py-3 text-left text-xs font-semibold uppercase text-slate-600"
                >
                  <span class="sr-only">Ações</span>
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              @for (operador of operadores(); track operador.id) {
                <tr>
                  <td class="px-4 py-3 text-sm text-slate-900">{{ operador.nome }}</td>
                  <td class="px-4 py-3 text-sm text-slate-700">{{ operador.usuario }}</td>
                  <td class="px-4 py-3 text-sm text-slate-700">{{ operador.email }}</td>
                  <td class="px-4 py-3 text-sm">
                    <span
                      [class]="operador.ativo ? 'font-medium text-emerald-800' : 'text-slate-500'"
                    >
                      {{ operador.ativo ? 'Ativo' : 'Inativo' }}
                    </span>
                    @if (operador.ativo && operador.trocaSenhaPendente) {
                      <span
                        class="ml-2 rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-900"
                      >
                        Troca de senha pendente
                      </span>
                    }
                  </td>
                  <td class="px-4 py-3 text-right text-sm">
                    <div class="flex flex-wrap items-center justify-end gap-3">
                      <a
                        [routerLink]="['/organizacao/equipe', operador.id, 'editar']"
                        class="font-medium text-emerald-700 underline"
                      >
                        Editar
                      </a>

                      @if (operador.ativo) {
                        @if (confirmandoId() === operador.id) {
                          <span class="text-slate-700">Inativar esta conta?</span>
                          <button
                            type="button"
                            class="font-medium text-red-700 underline disabled:opacity-60"
                            [disabled]="inativandoId() === operador.id"
                            (click)="inativar(operador)"
                          >
                            {{ inativandoId() === operador.id ? 'Inativando…' : 'Confirmar' }}
                          </button>
                          <button
                            type="button"
                            class="font-medium text-slate-600 underline"
                            [disabled]="inativandoId() === operador.id"
                            (click)="cancelarConfirmacao()"
                          >
                            Cancelar
                          </button>
                        } @else {
                          <button
                            type="button"
                            class="font-medium text-red-700 underline"
                            (click)="confirmarInativacao(operador.id)"
                          >
                            Inativar
                          </button>
                        }
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
})
export class EquipePage implements OnInit {
  private readonly equipe = inject(EquipeService);
  private readonly rota = inject(ActivatedRoute);

  protected readonly operadores = signal<ContaDaEquipe[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erroDeCarga = signal<string | null>(null);
  protected readonly erroDeAcao = signal<string | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly confirmandoId = signal<string | null>(null);
  protected readonly inativandoId = signal<string | null>(null);

  constructor() {
    const parametros = this.rota.snapshot.queryParamMap;

    if (parametros.get('criado') === '1') {
      this.mensagem.set('Operador criado. A senha inicial deve ser alterada no primeiro acesso.');
    } else if (parametros.get('editado') === '1') {
      this.mensagem.set('Operador atualizado.');
    }
  }

  ngOnInit(): void {
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroDeCarga.set(null);

    this.equipe.listar().subscribe({
      next: (operadores) => {
        this.operadores.set(operadores);
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.carregando.set(false);
        this.erroDeCarga.set(mensagemDeErro(erro));
      },
    });
  }

  protected confirmarInativacao(id: string): void {
    this.erroDeAcao.set(null);
    this.confirmandoId.set(id);
  }

  protected cancelarConfirmacao(): void {
    this.confirmandoId.set(null);
  }

  protected inativar(operador: ContaDaEquipe): void {
    this.inativandoId.set(operador.id);
    this.erroDeAcao.set(null);
    this.mensagem.set(null);

    this.equipe.inativar(operador.id).subscribe({
      next: () => {
        this.inativandoId.set(null);
        this.confirmandoId.set(null);
        this.mensagem.set('Operador inativado.');
        this.carregar();
      },
      error: (erro: unknown) => {
        this.inativandoId.set(null);
        this.confirmandoId.set(null);
        this.erroDeAcao.set(mensagemDeErro(erro));
      },
    });
  }
}
