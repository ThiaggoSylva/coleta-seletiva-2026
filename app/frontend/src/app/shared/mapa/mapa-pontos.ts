import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  input,
  output,
  viewChild,
} from '@angular/core';
import * as L from 'leaflet';

export interface MarcadorDoMapa {
  id: string;
  nome: string;
  latitude: number;
  longitude: number;
  descricao?: string;
}

export interface Coordenada {
  latitude: number;
  longitude: number;
}

/** Mapa OpenStreetMap com marcadores; também serve como seletor de coordenadas. */
@Component({
  selector: 'app-mapa-pontos',
  template: `<div
    #area
    class="h-80 w-full rounded-md border border-slate-200 bg-slate-100"
    role="application"
    [attr.aria-label]="rotulo()"
  ></div>`,
})
export class MapaPontos implements AfterViewInit, OnDestroy {
  readonly marcadores = input<readonly MarcadorDoMapa[]>([]);
  readonly centro = input<Coordenada | null>(null);
  readonly zoom = input(13);
  readonly interativo = input(false);
  readonly rotulo = input('Mapa de pontos de coleta');
  readonly pontoEscolhido = output<Coordenada>();

  private readonly area = viewChild.required<ElementRef<HTMLDivElement>>('area');
  private mapa: L.Map | null = null;
  private camada: L.LayerGroup | null = null;
  private marcadorDoPonto: L.Marker | null = null;

  constructor() {
    effect(() => {
      const marcadores = this.marcadores();
      if (this.mapa !== null) {
        this.desenhar(marcadores);
      }
    });

    effect(() => {
      const centro = this.centro();
      if (this.mapa === null || centro === null) {
        return;
      }

      this.mapa.setView([centro.latitude, centro.longitude], this.zoom());
      if (this.interativo()) {
        this.posicionarMarcador(centro.latitude, centro.longitude);
      }
    });
  }

  ngAfterViewInit(): void {
    this.mapa = L.map(this.area().nativeElement, { scrollWheelZoom: false }).setView(
      [-15.78, -47.93],
      4,
    );

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    }).addTo(this.mapa);

    this.camada = L.layerGroup().addTo(this.mapa);

    this.mapa.on('click', (evento: L.LeafletMouseEvent) => {
      if (!this.interativo()) {
        return;
      }

      this.posicionarMarcador(evento.latlng.lat, evento.latlng.lng);
      this.pontoEscolhido.emit({ latitude: evento.latlng.lat, longitude: evento.latlng.lng });
    });

    const centro = this.centro();
    if (centro !== null) {
      this.mapa.setView([centro.latitude, centro.longitude], this.zoom());
    }

    this.desenhar(this.marcadores());
  }

  ngOnDestroy(): void {
    this.mapa?.remove();
    this.mapa = null;
  }

  private desenhar(marcadores: readonly MarcadorDoMapa[]): void {
    if (this.camada === null || this.mapa === null) {
      return;
    }

    this.camada.clearLayers();
    for (const marcador of marcadores) {
      const descricao =
        marcador.descricao === undefined ? '' : `<br>${escaparHtml(marcador.descricao)}`;
      L.marker([marcador.latitude, marcador.longitude], { icon: this.icone() })
        .bindPopup(`<strong>${escaparHtml(marcador.nome)}</strong>${descricao}`)
        .addTo(this.camada);
    }

    if (marcadores.length > 0 && this.centro() === null) {
      const limites = L.latLngBounds(
        marcadores.map((marcador) => [marcador.latitude, marcador.longitude] as [number, number]),
      );
      this.mapa.fitBounds(limites.pad(0.2), { maxZoom: 14 });
    }
  }

  private posicionarMarcador(latitude: number, longitude: number): void {
    if (this.mapa === null) {
      return;
    }

    this.marcadorDoPonto?.remove();
    this.marcadorDoPonto = L.marker([latitude, longitude], { icon: this.icone() }).addTo(this.mapa);
  }

  private icone(): L.DivIcon {
    return L.divIcon({
      className: '',
      html: '<span class="block h-4 w-4 rounded-full border-2 border-white bg-emerald-700 shadow"></span>',
      iconSize: [16, 16],
      iconAnchor: [8, 8],
    });
  }
}

function escaparHtml(texto: string): string {
  return texto
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
